using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace GestionTime.Desktop.Services;

/// <summary>
/// HTTP logging handler:
/// - Log request/response line + status + duration.
/// - Logs bodies (truncated) but redacts secrets (password/token/cookies).
/// - Does NOT break downstream reads (replaces response content after reading).
/// </summary>
public sealed class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger _log;
    private readonly int _maxBodyChars;

    public HttpLoggingHandler(ILogger log, int maxBodyChars = 2000)
    {
        _log = log;
        _maxBodyChars = Math.Max(200, maxBodyChars);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        var sw = Stopwatch.StartNew();

        var url = request.RequestUri?.ToString() ?? "(null url)";
        _log.LogInformation("[HTTP {id}] --> {method} {url}", id, request.Method, url);

        LogHeaders(id, "RequestHeaders", request.Headers);
        if (request.Content?.Headers != null)
            LogHeaders(id, "RequestContentHeaders", request.Content.Headers);

        // BODY request
        if (request.Content != null)
        {
            var originalContent = request.Content;
            var body = await SafeReadContentAsStringAsync(originalContent, ct);
            var safeBody = Truncate(Redact(body), _maxBodyChars);
            _log.LogDebug("[HTTP {id}] RequestBody: {body}", id, safeBody);

            // Re-create content so downstream can still read it (and to be safe with non-buffered content).
            request.Content = CloneContent(originalContent, body);
        }

        try
        {
            var resp = await base.SendAsync(request, ct);
            sw.Stop();

            _log.LogInformation("[HTTP {id}] <-- {status} {reason} ({ms} ms)",
                id, (int)resp.StatusCode, resp.ReasonPhrase, sw.ElapsedMilliseconds);

            LogHeaders(id, "ResponseHeaders", resp.Headers);
            if (resp.Content?.Headers != null)
                LogHeaders(id, "ResponseContentHeaders", resp.Content.Headers);

            // BODY response
            if (resp.Content != null)
            {
                var original = resp.Content;
                var respBody = await SafeReadContentAsStringAsync(original, ct);
                var safeRespBody = Truncate(Redact(respBody), _maxBodyChars);
                _log.LogDebug("[HTTP {id}] ResponseBody: {body}", id, safeRespBody);

                // replace so other code can read it again
                resp.Content = CloneContent(original, respBody);
            }

            return resp;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError(ex, "[HTTP {id}] xx ERROR ({ms} ms) {msg}", id, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }

    private void LogHeaders(string id, string label, HttpHeaders headers)
    {
        try
        {
            // avoid leaking secrets
            foreach (var h in headers)
            {
                var name = h.Key;
                if (IsSecretHeader(name))
                {
                    _log.LogDebug("[HTTP {id}] {label}: {name} = ***", id, label, name);
                    continue;
                }

                var value = string.Join("; ", h.Value);
                _log.LogDebug("[HTTP {id}] {label}: {name} = {value}", id, label, name, value);
            }
        }
        catch
        {
            // never break request due to logging
        }
    }

    private static bool IsSecretHeader(string name)
        => name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
           || name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase);

    private static string Truncate(string text, int max)
        => text.Length <= max ? text : text[..max] + "…(trunc)";

    private static async Task<string> SafeReadContentAsStringAsync(HttpContent content, CancellationToken ct)
    {
        try
        {
            return await content.ReadAsStringAsync(ct);
        }
        catch
        {
            return "(unreadable content)";
        }
    }

    private static HttpContent CloneContent(HttpContent original, string body)
    {
        var mediaType = original.Headers.ContentType?.MediaType ?? "text/plain";
        var charset = original.Headers.ContentType?.CharSet;
        var encoding = !string.IsNullOrWhiteSpace(charset)
            ? Encoding.GetEncoding(charset)
            : Encoding.UTF8;

        var clone = new StringContent(body, encoding, mediaType);

        // copy other content headers
        foreach (var h in original.Headers)
        {
            // content-type already set by StringContent
            if (h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;

            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }

        return clone;
    }

    private static string Redact(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return body;

        // JSON redaction
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
        {
            try
            {
                var node = JsonNode.Parse(body);
                RedactNode(node);
                return node?.ToJsonString(new JsonSerializerOptions
                {
                    WriteIndented = false
                }) ?? body;
            }
            catch
            {
                // fallthrough
            }
        }

        // Fallback: minimal redaction for obvious patterns
        return body
            .Replace("\"password\":\"", "\"password\":\"***", StringComparison.OrdinalIgnoreCase)
            .Replace("\"pass\":\"", "\"pass\":\"***", StringComparison.OrdinalIgnoreCase)
            .Replace("\"token\":\"", "\"token\":\"***", StringComparison.OrdinalIgnoreCase);
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is null) return;

        if (node is JsonObject obj)
        {
            foreach (var kv in obj.ToList())
            {
                var key = kv.Key;
                var child = kv.Value;

                if (IsSecretKey(key))
                {
                    obj[key] = "***";
                }
                else
                {
                    RedactNode(child);
                }
            }
            return;
        }

        if (node is JsonArray arr)
        {
            foreach (var child in arr)
                RedactNode(child);
        }
    }

    private static bool IsSecretKey(string key)
    {
        // login payloads
        if (key.Equals("password", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("pass", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("pwd", StringComparison.OrdinalIgnoreCase)) return true;

        // tokens
        if (key.Equals("token", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("accessToken", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("access_token", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("refreshToken", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("refresh_token", StringComparison.OrdinalIgnoreCase)) return true;
        if (key.Equals("jwt", StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }
}

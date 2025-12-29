using System;
using System.Text.RegularExpressions;

namespace GestionTime.Desktop.Diagnostics;

/// <summary>
/// Utilidades para sanitizar información sensible en logs
/// </summary>
public static class LogSanitizer
{
    private static readonly string[] SensitiveFields = 
    {
        "password", "token", "authorization", "secret", "key", "credential",
        "passwd", "pwd", "auth", "bearer", "jwt", "session", "cookie"
    };
    
    private static readonly string[] SensitivePatterns = 
    {
        // Emails (parcialmente ocultos)
        @"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
        
        // IPs (último octeto oculto)
        @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.)\d{1,3}",
        
        // URLs con tokens
        @"(token=|jwt=|auth=)([^&\s]+)"
    };
    
    /// <summary>
    /// Sanitiza JSON ocultando campos sensibles
    /// </summary>
    public static string SanitizeJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;
            
        var sanitized = json;
        
        foreach (var field in SensitiveFields)
        {
            // Pattern para JSON: "campo":"valor"
            var pattern = $"\"{field}\"\\s*:\\s*\"([^\"]*?)\"";
            sanitized = Regex.Replace(sanitized, pattern, $"\"{field}\":\"***\"", RegexOptions.IgnoreCase);
            
            // Pattern para JSON sin comillas: "campo":valor
            pattern = $"\"{field}\"\\s*:\\s*([^,\\}}\\s]+)";
            sanitized = Regex.Replace(sanitized, pattern, $"\"{field}\":\"***\"", RegexOptions.IgnoreCase);
        }
        
        return sanitized;
    }
    
    /// <summary>
    /// Sanitiza texto general ocultando información sensible
    /// </summary>
    public static string SanitizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;
            
        var sanitized = text;
        
        // Ocultar emails parcialmente: usuario@domain.com -> u***o@d***n.com
        sanitized = Regex.Replace(sanitized, 
            @"([a-zA-Z0-9._%+-]+)@([a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
            match => ObfuscateEmail(match.Groups[1].Value, match.Groups[2].Value));
        
        // Ocultar último octeto de IPs: 192.168.1.100 -> 192.168.1.***
        sanitized = Regex.Replace(sanitized,
            @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.)\d{1,3}",
            "$1***");
            
        // Ocultar tokens en URLs
        sanitized = Regex.Replace(sanitized,
            @"(token=|jwt=|auth=)([^&\s]+)",
            "$1***");
        
        return sanitized;
    }
    
    /// <summary>
    /// Ofusca email manteniendo estructura reconocible
    /// </summary>
    private static string ObfuscateEmail(string user, string domain)
    {
        if (user.Length <= 2)
            return $"***@{ObfuscateDomain(domain)}";
            
        var obfuscatedUser = user.Length > 3 
            ? $"{user[0]}***{user[^1]}" 
            : $"{user[0]}***";
            
        return $"{obfuscatedUser}@{ObfuscateDomain(domain)}";
    }
    
    /// <summary>
    /// Ofusca dominio manteniendo estructura
    /// </summary>
    private static string ObfuscateDomain(string domain)
    {
        var parts = domain.Split('.');
        if (parts.Length < 2)
            return "***";
            
        var obfuscatedParts = new string[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            obfuscatedParts[i] = part.Length > 2 
                ? $"{part[0]}***{part[^1]}" 
                : "***";
        }
        
        return string.Join(".", obfuscatedParts);
    }
    
    /// <summary>
    /// Trunca texto largo para logs
    /// </summary>
    public static string Truncate(string text, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
            
        return text.Substring(0, maxLength) + "... [TRUNCATED]";
    }
}
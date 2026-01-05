using System;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta del endpoint GET /api/v1/profiles (perfil completo del usuario).</summary>
public sealed class UserProfileResponse
{
    /// <summary>ID del perfil (UUID string del backend).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>ID del usuario (no presente en la respuesta actual del backend).</summary>
    [JsonPropertyName("user_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? UserId { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    /// <summary>Nombre completo devuelto por el backend.</summary>
    [JsonPropertyName("full_name")]
    public string? FullNameFromBackend { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("employee_type")]
    public string? EmployeeType { get; set; }

    [JsonPropertyName("hire_date")]
    public DateTime? HireDate { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>Nombre completo del usuario (prioriza backend, luego construye).</summary>
    [JsonIgnore]
    public string FullName => !string.IsNullOrWhiteSpace(FullNameFromBackend) 
        ? FullNameFromBackend 
        : $"{FirstName} {LastName}".Trim();

    /// <summary>Indica si el perfil está completo (tiene nombre y apellido).</summary>
    [JsonIgnore]
    public bool IsComplete => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
}

/// <summary>Request DTO para actualizar el perfil del usuario (PUT /api/v1/profiles).</summary>
public sealed class UpdateProfileRequest
{
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("department")]
    public string? Department { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("employee_type")]
    public string? EmployeeType { get; set; }

    [JsonPropertyName("hire_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HireDate { get; set; } // Formato: "YYYY-MM-DD" o ISO 8601

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GestionTime.Desktop.Models.Dtos;

/// <summary>Respuesta paginada de usuarios desde /api/v1/users</summary>
public sealed class UsersPagedResponse
{
    [JsonPropertyName("users")]
    public List<UserListItemDto> Users { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}

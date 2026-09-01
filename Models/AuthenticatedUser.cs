using System;
using GestionTime.Desktop.Models.Enums;

namespace GestionTime.Desktop.Models;

/// <summary>Sesión autenticada en memoria (no usar el ID de perfil).</summary>
public sealed class AuthenticatedUser
{
    /// <summary>Identificador del usuario autenticado (LoginResponse.User.Id).</summary>
    public Guid UserId { get; init; }

    /// <summary>Email de la sesión.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Nombre visible.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Rol normalizado de la sesión.</summary>
    public UserRole Role { get; init; } = UserRole.USER;

    /// <summary>Indica si el rol es ADMIN.</summary>
    public bool IsAdmin => Role == UserRole.ADMIN;
}

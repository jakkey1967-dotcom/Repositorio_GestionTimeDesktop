using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GestionTime.Desktop.Helpers;

/// <summary>Gestiona el almacenamiento de información de usuario en archivos JSON locales.</summary>
public static class UserInfoFileStorage
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GestionTime"
    );
    
    private static readonly string UserInfoFilePath = Path.Combine(AppDataPath, "user-info.json");
    
    /// <summary>Modelo de datos para la información del usuario.</summary>
    public sealed class UserInfo
    {
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? UserAvatar { get; set; }
        public DateTime? LastUpdated { get; set; }
    }
    
    /// <summary>Guarda la información del usuario en archivo JSON.</summary>
    public static bool SaveUserInfo(string? userName, string? userEmail, string? userRole, string? userAvatar = null, ILogger? log = null)
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);
            
            var userInfo = new UserInfo
            {
                UserName = userName,
                UserEmail = userEmail,
                UserRole = userRole,
                UserAvatar = userAvatar,
                LastUpdated = DateTime.Now
            };
            
            var json = JsonSerializer.Serialize(userInfo, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            File.WriteAllText(UserInfoFilePath, json);
            
            log?.LogInformation("💾 Información de usuario guardada en: {path}", UserInfoFilePath);
            log?.LogDebug("   • UserName: {name}", userName);
            log?.LogDebug("   • UserEmail: {email}", userEmail);
            log?.LogDebug("   • UserRole: {role}", userRole);
            
            return true;
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error guardando información de usuario en archivo");
            return false;
        }
    }
    
    /// <summary>Carga la información del usuario desde archivo JSON.</summary>
    public static UserInfo? LoadUserInfo(ILogger? log = null)
    {
        try
        {
            if (!File.Exists(UserInfoFilePath))
            {
                log?.LogDebug("📂 Archivo de usuario no existe: {path}", UserInfoFilePath);
                return null;
            }
            
            var json = File.ReadAllText(UserInfoFilePath);
            var userInfo = JsonSerializer.Deserialize<UserInfo>(json);
            
            if (userInfo != null)
            {
                log?.LogInformation("📥 Información de usuario cargada desde archivo");
                log?.LogDebug("   • UserName: {name}", userInfo.UserName);
                log?.LogDebug("   • UserEmail: {email}", userInfo.UserEmail);
                log?.LogDebug("   • UserRole: {role}", userInfo.UserRole);
                log?.LogDebug("   • LastUpdated: {date}", userInfo.LastUpdated);
            }
            
            return userInfo;
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error cargando información de usuario desde archivo");
            return null;
        }
    }
    
    /// <summary>Elimina el archivo de información de usuario.</summary>
    public static bool ClearUserInfo(ILogger? log = null)
    {
        try
        {
            if (File.Exists(UserInfoFilePath))
            {
                File.Delete(UserInfoFilePath);
                log?.LogInformation("🗑️ Archivo de información de usuario eliminado");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error eliminando archivo de información de usuario");
            return false;
        }
    }
    
    /// <summary>Actualiza solo el UserName sin afectar otros campos.</summary>
    public static bool UpdateUserName(string userName, ILogger? log = null)
    {
        try
        {
            var userInfo = LoadUserInfo(log);
            if (userInfo == null)
            {
                return SaveUserInfo(userName, null, null, null, log);
            }
            
            userInfo.UserName = userName;
            userInfo.LastUpdated = DateTime.Now;
            
            return SaveUserInfo(userInfo.UserName, userInfo.UserEmail, userInfo.UserRole, userInfo.UserAvatar, log);
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error actualizando UserName");
            return false;
        }
    }
    
    /// <summary>Actualiza solo el UserRole sin afectar otros campos.</summary>
    public static bool UpdateUserRole(string userRole, ILogger? log = null)
    {
        try
        {
            var userInfo = LoadUserInfo(log);
            if (userInfo == null)
            {
                return SaveUserInfo(null, null, userRole, null, log);
            }
            
            userInfo.UserRole = userRole;
            userInfo.LastUpdated = DateTime.Now;
            
            return SaveUserInfo(userInfo.UserName, userInfo.UserEmail, userInfo.UserRole, userInfo.UserAvatar, log);
        }
        catch (Exception ex)
        {
            log?.LogError(ex, "❌ Error actualizando UserRole");
            return false;
        }
    }
}

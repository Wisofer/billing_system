namespace billing_system.Utils;

/// <summary>
/// Configuración para JWT Authentication (API Móvil)
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Clave secreta para firmar los tokens
    /// </summary>
    public string Key { get; set; } = string.Empty;
    
    /// <summary>
    /// Emisor del token (nombre de la aplicación)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;
    
    /// <summary>
    /// Audiencia del token (app móvil)
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// Horas de expiración del token
    /// </summary>
    public int ExpirationHours { get; set; } = 168; // 7 días por defecto
}


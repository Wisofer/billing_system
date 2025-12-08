namespace billing_system.Models.Entities;

/// <summary>
/// Representa un método de pago (cuenta bancaria) para la landing page
/// </summary>
public class MetodoPago
{
    public int Id { get; set; }
    
    /// <summary>
    /// Nombre del banco (Banpro, Lafise, BAC, etc.)
    /// </summary>
    public string NombreBanco { get; set; } = string.Empty;
    
    /// <summary>
    /// Icono o emoji del banco (🏦, 🏛️, 💳, 📱)
    /// </summary>
    public string? Icono { get; set; }
    
    /// <summary>
    /// Tipo de cuenta (Córdobas, Dólares, Billetera Móvil)
    /// </summary>
    public string TipoCuenta { get; set; } = string.Empty;
    
    /// <summary>
    /// Moneda de la cuenta (C$, $, 📱)
    /// </summary>
    public string Moneda { get; set; } = string.Empty;
    
    /// <summary>
    /// Número de cuenta bancaria
    /// </summary>
    public string? NumeroCuenta { get; set; }
    
    /// <summary>
    /// Información adicional o mensaje (ej: "Próximamente", "Envía comprobante al...")
    /// </summary>
    public string? Mensaje { get; set; }
    
    /// <summary>
    /// Orden de visualización en la landing page
    /// </summary>
    public int Orden { get; set; } = 0;
    
    /// <summary>
    /// Si el método está activo y visible en la landing page
    /// </summary>
    public bool Activo { get; set; } = true;
    
    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }
}


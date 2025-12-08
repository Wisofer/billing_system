namespace billing_system.Models.Entities;

/// <summary>
/// Representa un servicio de internet mostrado en la landing page
/// Independiente de los servicios internos del sistema
/// </summary>
public class ServicioLandingPage
{
    public int Id { get; set; }
    
    /// <summary>
    /// Título del servicio (ej: "Plan de hasta 10Mbps")
    /// </summary>
    public string Titulo { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción del servicio (ej: "Servicio de Internet Residencial hasta 10mbps")
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;
    
    /// <summary>
    /// Precio del servicio en córdobas
    /// </summary>
    public decimal Precio { get; set; }
    
    /// <summary>
    /// Velocidad del servicio (ej: "10Mbps", "20Mbps")
    /// </summary>
    public string? Velocidad { get; set; }
    
    /// <summary>
    /// Etiqueta especial (ej: "OFERTA DICIEMBRE", "RECOMENDADO")
    /// </summary>
    public string? Etiqueta { get; set; }
    
    /// <summary>
    /// Color de la etiqueta en formato Tailwind (ej: "bg-red-500", "bg-green-500")
    /// </summary>
    public string? ColorEtiqueta { get; set; }
    
    /// <summary>
    /// Icono o emoji para el servicio
    /// </summary>
    public string? Icono { get; set; }
    
    /// <summary>
    /// Características adicionales del servicio (JSON array de strings)
    /// </summary>
    public string? Caracteristicas { get; set; }
    
    /// <summary>
    /// Orden de visualización en la landing page
    /// </summary>
    public int Orden { get; set; } = 0;
    
    /// <summary>
    /// Si el servicio está activo y visible en la landing page
    /// </summary>
    public bool Activo { get; set; } = true;
    
    /// <summary>
    /// Si el servicio es destacado (aparece con diseño especial)
    /// </summary>
    public bool Destacado { get; set; } = false;
    
    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }
}


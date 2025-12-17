namespace billing_system.Models.Entities;

/// <summary>
/// Representa un egreso/gasto del negocio
/// Permite registrar todos los gastos operativos para calcular el balance real
/// </summary>
public class Egreso
{
    public int Id { get; set; }
    
    /// <summary>
    /// Código único del egreso (EGR-0001, EGR-0002...)
    /// </summary>
    public string Codigo { get; set; } = string.Empty;
    
    /// <summary>
    /// Descripción del gasto
    /// </summary>
    public string Descripcion { get; set; } = string.Empty;
    
    /// <summary>
    /// Categoría del egreso
    /// </summary>
    public string Categoria { get; set; } = string.Empty;
    
    /// <summary>
    /// Monto del egreso en córdobas
    /// </summary>
    public decimal Monto { get; set; }
    
    /// <summary>
    /// Fecha en que se realizó el gasto
    /// </summary>
    public DateTime Fecha { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Número de factura o recibo del proveedor (opcional)
    /// </summary>
    public string? NumeroFactura { get; set; }
    
    /// <summary>
    /// Proveedor o beneficiario del pago (opcional)
    /// </summary>
    public string? Proveedor { get; set; }
    
    /// <summary>
    /// Método de pago utilizado
    /// </summary>
    public string MetodoPago { get; set; } = "Efectivo";
    
    /// <summary>
    /// Observaciones adicionales
    /// </summary>
    public string? Observaciones { get; set; }
    
    /// <summary>
    /// Usuario que registró el egreso
    /// </summary>
    public int? UsuarioId { get; set; }
    
    /// <summary>
    /// Si el egreso está activo (para soft delete)
    /// </summary>
    public bool Activo { get; set; } = true;
    
    /// <summary>
    /// Fecha de creación del registro
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Fecha de última actualización
    /// </summary>
    public DateTime? FechaActualizacion { get; set; }
    
    // Relaciones
    public virtual Usuario? Usuario { get; set; }
}

/// <summary>
/// Categorías predefinidas de egresos
/// </summary>
public static class CategoriasEgreso
{
    public const string PagoInternet = "Pago de Internet";
    public const string Sueldos = "Sueldos y Salarios";
    public const string Mantenimiento = "Mantenimiento de Equipos";
    public const string CompraEquipos = "Compra de Equipos";
    public const string CompraMateriales = "Compra de Materiales";
    public const string ServiciosPublicos = "Servicios Públicos";
    public const string Alquiler = "Alquiler";
    public const string Transporte = "Transporte";
    public const string Publicidad = "Publicidad";
    public const string Otros = "Otros";
    
    public static List<string> ObtenerTodas()
    {
        return new List<string>
        {
            PagoInternet,
            Sueldos,
            Mantenimiento,
            CompraEquipos,
            CompraMateriales,
            ServiciosPublicos,
            Alquiler,
            Transporte,
            Publicidad,
            Otros
        };
    }
}

/// <summary>
/// Métodos de pago para egresos
/// </summary>
public static class MetodosPagoEgreso
{
    public const string Efectivo = "Efectivo";
    public const string Transferencia = "Transferencia";
    public const string Cheque = "Cheque";
    public const string TarjetaCredito = "Tarjeta de Crédito";
    public const string TarjetaDebito = "Tarjeta de Débito";
    
    public static List<string> ObtenerTodos()
    {
        return new List<string>
        {
            Efectivo,
            Transferencia,
            Cheque,
            TarjetaCredito,
            TarjetaDebito
        };
    }
}


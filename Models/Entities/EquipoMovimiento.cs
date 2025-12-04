namespace billing_system.Models.Entities;

/// <summary>
/// Tabla intermedia para relacionar un MovimientoInventario con múltiples Equipos
/// </summary>
public class EquipoMovimiento
{
    public int Id { get; set; }
    public int MovimientoInventarioId { get; set; }
    public int EquipoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal? PrecioUnitario { get; set; }
    public int? UbicacionOrigenId { get; set; }
    public int? UbicacionDestinoId { get; set; }
    
    // Relaciones
    public virtual MovimientoInventario MovimientoInventario { get; set; } = null!;
    public virtual Equipo Equipo { get; set; } = null!;
    public virtual Ubicacion? UbicacionOrigen { get; set; }
    public virtual Ubicacion? UbicacionDestino { get; set; }
}


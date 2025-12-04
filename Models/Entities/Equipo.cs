namespace billing_system.Models.Entities;

public class Equipo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty; // INV_0001, INV_0002...
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? NumeroSerie { get; set; } // MAC Address, Serial Number
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public int CategoriaEquipoId { get; set; }
    public int UbicacionId { get; set; }
    public string Estado { get; set; } = "Disponible"; // Disponible, En uso, Dañado, En reparación, Retirado
    public int Stock { get; set; } = 0; // Cantidad disponible
    public int StockMinimo { get; set; } = 0; // Alerta cuando llegue a este nivel
    public decimal? PrecioCompra { get; set; }
    public DateTime? FechaAdquisicion { get; set; }
    public int? ProveedorId { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaActualizacion { get; set; }
    
    // Relaciones
    public virtual CategoriaEquipo CategoriaEquipo { get; set; } = null!;
    public virtual Ubicacion Ubicacion { get; set; } = null!;
    public virtual Proveedor? Proveedor { get; set; }
    public virtual ICollection<MovimientoInventario> MovimientosInventario { get; set; } = new List<MovimientoInventario>();
    public virtual ICollection<AsignacionEquipo> AsignacionesEquipo { get; set; } = new List<AsignacionEquipo>();
    public virtual ICollection<MantenimientoReparacion> MantenimientosReparaciones { get; set; } = new List<MantenimientoReparacion>();
    public virtual ICollection<HistorialEstadoEquipo> HistorialEstados { get; set; } = new List<HistorialEstadoEquipo>();
}


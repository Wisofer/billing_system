namespace billing_system.Models.Entities;

public class MantenimientoReparacion
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public string Tipo { get; set; } = string.Empty; // Preventivo, Correctivo
    public DateTime? FechaProgramada { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? ProveedorTecnico { get; set; } // Nombre del proveedor o técnico
    public decimal? Costo { get; set; }
    public string? ProblemaReportado { get; set; }
    public string? SolucionAplicada { get; set; }
    public string Estado { get; set; } = "Programado"; // Programado, En proceso, Completado, Cancelado
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual Equipo Equipo { get; set; } = null!;
}


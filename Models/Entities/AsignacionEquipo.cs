namespace billing_system.Models.Entities;

public class AsignacionEquipo
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public int Cantidad { get; set; } = 1;
    public int? ClienteId { get; set; } // Opcional: asignar a cliente
    public string? EmpleadoNombre { get; set; } // Nombre del empleado/técnico asignado
    public DateTime FechaAsignacion { get; set; } = DateTime.Now;
    public DateTime? FechaDevolucionEsperada { get; set; }
    public DateTime? FechaDevolucionReal { get; set; }
    public string Estado { get; set; } = "Activa"; // Activa, Devuelta, Perdida
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual Equipo Equipo { get; set; } = null!;
    public virtual Cliente? Cliente { get; set; }
}


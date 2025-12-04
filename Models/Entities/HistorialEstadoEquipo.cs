namespace billing_system.Models.Entities;

/// <summary>
/// Historial de cambios de estado de un equipo (auditoría)
/// </summary>
public class HistorialEstadoEquipo
{
    public int Id { get; set; }
    public int EquipoId { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public DateTime FechaCambio { get; set; } = DateTime.Now;
    public int UsuarioId { get; set; } // Usuario que hizo el cambio
    public string? Motivo { get; set; }
    public string? Observaciones { get; set; }
    
    // Relaciones
    public virtual Equipo Equipo { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}


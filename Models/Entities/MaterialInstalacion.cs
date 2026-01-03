namespace billing_system.Models.Entities;

/// <summary>
/// Registra los materiales utilizados en una instalación de cliente
/// Permite descontar automáticamente del inventario al crear/editar instalaciones
/// </summary>
public class MaterialInstalacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; } // Cliente al que se le hizo la instalación
    public int EquipoId { get; set; } // Material/equipo utilizado
    public decimal Cantidad { get; set; } // Cantidad usada (puede ser decimal para metros)
    public DateTime FechaInstalacion { get; set; } = DateTime.Now;
    public string? Observaciones { get; set; } // Notas sobre el uso del material
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual Equipo Equipo { get; set; } = null!;
}


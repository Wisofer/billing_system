namespace billing_system.Models.Entities;

public class CategoriaEquipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Router, Antena, Cable, Switch, etc.
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}


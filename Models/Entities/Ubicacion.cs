namespace billing_system.Models.Entities;

public class Ubicacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Almacén Principal, Almacén Secundario, En Campo, etc.
    public string? Direccion { get; set; }
    public string Tipo { get; set; } = "Almacen"; // Almacen, Campo, Reparacion
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}


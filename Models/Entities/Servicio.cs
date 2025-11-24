namespace billing_system.Models.Entities;

public class Servicio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public string Categoria { get; set; } = "Internet"; // "Internet" o "Streaming"
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
    public virtual ICollection<ClienteServicio> ClienteServicios { get; set; } = new List<ClienteServicio>(); // Relación muchos-a-muchos
}


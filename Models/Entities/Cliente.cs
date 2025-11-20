namespace billing_system.Models.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Cedula { get; set; }
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual ICollection<Factura> Facturas { get; set; } = new List<Factura>();
    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}


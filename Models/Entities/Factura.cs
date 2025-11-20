namespace billing_system.Models.Entities;

public class Factura
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty; // # de factura
    public int ClienteId { get; set; }
    public int ServicioId { get; set; }
    public decimal Monto { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagada, Cancelada
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime MesFacturacion { get; set; } // Mes al que corresponde la factura
    public string? ArchivoPDF { get; set; } // Ruta del archivo PDF
    
    // Relaciones
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual Servicio Servicio { get; set; } = null!;
    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}


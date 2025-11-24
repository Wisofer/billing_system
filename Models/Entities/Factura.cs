namespace billing_system.Models.Entities;

public class Factura
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty; // # de factura
    public int ClienteId { get; set; }
    public int ServicioId { get; set; } // Servicio principal (para compatibilidad)
    public string Categoria { get; set; } = "Internet"; // "Internet" o "Streaming" - categoría de la factura
    public decimal Monto { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Pendiente, Pagada, Cancelada
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime MesFacturacion { get; set; } // Mes al que corresponde la factura
    public string? ArchivoPDF { get; set; } // Ruta del archivo PDF
    
    // Relaciones
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual Servicio Servicio { get; set; } = null!; // Servicio principal (para compatibilidad)
    public virtual ICollection<FacturaServicio> FacturaServicios { get; set; } = new List<FacturaServicio>(); // Servicios incluidos en esta factura
    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}


namespace billing_system.Models.Entities;

public class FacturaServicio
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public int ServicioId { get; set; }
    public int Cantidad { get; set; } = 1; // Cantidad de servicios/suscripciones (Internet y Streaming)
    public decimal Monto { get; set; } // Monto individual de este servicio en la factura
    
    // Propiedades de navegación
    public virtual Factura Factura { get; set; } = null!;
    public virtual Servicio Servicio { get; set; } = null!;
}


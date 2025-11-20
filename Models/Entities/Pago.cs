namespace billing_system.Models.Entities;

public class Pago
{
    public int Id { get; set; }
    public int FacturaId { get; set; }
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "C$"; // C$, $, Ambos
    public string TipoPago { get; set; } = string.Empty; // Fisico, Electronico
    public string? Banco { get; set; } // Banpro, Lafise, BAC, Ficohsa, BDF
    public string? TipoCuenta { get; set; } // Cuenta $, Cuenta C$, Billetera movil
    public decimal? MontoRecibido { get; set; } // Para pago físico
    public decimal? Vuelto { get; set; } // Para pago físico
    public decimal? TipoCambio { get; set; } // Tipo de cambio usado (C$36.80 = $1)
    public DateTime FechaPago { get; set; } = DateTime.Now;
    public string? Observaciones { get; set; }
    
    // Relaciones
    public virtual Factura Factura { get; set; } = null!;
}


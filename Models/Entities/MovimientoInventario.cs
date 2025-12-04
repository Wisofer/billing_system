namespace billing_system.Models.Entities;

public class MovimientoInventario
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // Entrada, Salida
    public string Subtipo { get; set; } = string.Empty; // Compra, Venta, Asignacion, Devolucion, Ajuste, Daño, Transferencia
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int UsuarioId { get; set; } // Usuario que realizó el movimiento
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    
    // Relaciones
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual ICollection<EquipoMovimiento> EquipoMovimientos { get; set; } = new List<EquipoMovimiento>();
}


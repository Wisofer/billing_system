namespace billing_system.Models.ViewModels;

public class DashboardViewModel
{
    public decimal PagosPendientes { get; set; }
    public decimal PagosRealizados { get; set; }
    public decimal IngresoTotal { get; set; }
    public decimal IngresoFaltante { get; set; }
    public int TotalClientes { get; set; }
    public int TotalFacturas { get; set; }
    public int TotalPagos { get; set; }
}


namespace billing_system.Models.ViewModels;

public class DashboardViewModel
{
    // Estadísticas del mes actual
    public decimal IngresosMesActual { get; set; }
    public decimal EgresosMesActual { get; set; }
    public decimal BalanceMesActual { get; set; }
    public int FacturasMesActual { get; set; }
    public int PagosMesActual { get; set; }
    public string MesActualTexto { get; set; } = "";
    
    // Estadísticas generales (históricas)
    public decimal PagosPendientes { get; set; }
    public decimal PagosRealizados { get; set; }
    public decimal IngresoTotal { get; set; }
    public decimal IngresoFaltante { get; set; }
    public int TotalClientes { get; set; }
    public int TotalFacturas { get; set; }
    public int TotalPagos { get; set; }
    public int TotalClientesActivos { get; set; }
    
    // Estadísticas por categoría - Internet
    public decimal IngresosInternet { get; set; }
    public decimal PendientesInternet { get; set; }
    public int FacturasInternet { get; set; }
    public int FacturasPagadasInternet { get; set; }
    public int FacturasPendientesInternet { get; set; }
    
    // Estadísticas por categoría - Streaming
    public decimal IngresosStreaming { get; set; }
    public decimal PendientesStreaming { get; set; }
    public int FacturasStreaming { get; set; }
    public int FacturasPagadasStreaming { get; set; }
    public int FacturasPendientesStreaming { get; set; }
    
    // Estadísticas de clientes
    public int ClientesConInternet { get; set; }
    public int ClientesConStreaming { get; set; }
    public int ClientesConAmbos { get; set; }
    
    // Estadísticas de egresos (históricas)
    public decimal TotalEgresos { get; set; }
    public int CantidadEgresos { get; set; }
    
    // Balance (Ingresos - Egresos) histórico
    public decimal Balance => PagosRealizados - TotalEgresos;
    
    // Estadísticas mensuales (últimos 6 meses)
    public List<MesEstadistica> EstadisticasMensuales { get; set; } = new();
}

public class MesEstadistica
{
    public string Mes { get; set; } = "";
    public decimal IngresosInternet { get; set; }
    public decimal IngresosStreaming { get; set; }
    public int FacturasInternet { get; set; }
    public int FacturasStreaming { get; set; }
}


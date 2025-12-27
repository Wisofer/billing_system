namespace billing_system.Models.ViewModels;

public class ReporteMensualViewModel
{
    // Periodo del reporte
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string PeriodoTexto { get; set; } = "";
    
    // Resumen Ejecutivo
    public decimal TotalIngresos { get; set; }
    public decimal TotalEgresos { get; set; }
    public decimal Balance { get; set; }
    
    // Ingresos por Categoría
    public decimal IngresosInternet { get; set; }
    public decimal IngresosStreaming { get; set; }
    public decimal PendientesInternet { get; set; }
    public decimal PendientesStreaming { get; set; }
    
    // Facturas
    public int FacturasGeneradas { get; set; }
    public int FacturasPagadas { get; set; }
    public int FacturasPendientes { get; set; }
    public int FacturasInternet { get; set; }
    public int FacturasStreaming { get; set; }
    public List<FacturaReporteDto> ListaFacturas { get; set; } = new();
    
    // Pagos
    public int PagosRecibidos { get; set; }
    public decimal MontoPagos { get; set; }
    public List<PagoReporteDto> ListaPagos { get; set; } = new();
    
    // Egresos
    public int CantidadEgresos { get; set; }
    public List<EgresoReporteDto> ListaEgresos { get; set; } = new();
    
    // Clientes
    public int ClientesNuevos { get; set; }
    public int ClientesActivos { get; set; }
    public List<ClienteNuevoDto> ListaClientesNuevos { get; set; } = new();
    
    // Comparación con mes anterior
    public decimal DiferenciaIngresos { get; set; }
    public decimal PorcentajeVariacionIngresos { get; set; }
    public decimal DiferenciaEgresos { get; set; }
    public decimal PorcentajeVariacionEgresos { get; set; }
}

public class FacturaReporteDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public string ClienteCodigo { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string Estado { get; set; } = "";
    public string Categoria { get; set; } = "";
}

public class PagoReporteDto
{
    public int Id { get; set; }
    public string ClienteCodigo { get; set; } = "";
    public string ClienteNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "";
    public string NumeroReferencia { get; set; } = "";
}

public class EgresoReporteDto
{
    public int Id { get; set; }
    public string Concepto { get; set; } = "";
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public string Categoria { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

public class ClienteNuevoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public string Servicios { get; set; } = "";
}



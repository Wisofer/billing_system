using billing_system.Models.ViewModels;

namespace billing_system.Services.IServices;

public interface IReporteService
{
    /// <summary>
    /// Genera un reporte mensual para un periodo específico
    /// </summary>
    ReporteMensualViewModel GenerarReporteMensual(DateTime fechaInicio, DateTime fechaFin);
    
    /// <summary>
    /// Genera un reporte para un mes específico
    /// </summary>
    ReporteMensualViewModel GenerarReportePorMes(int año, int mes);
    
    /// <summary>
    /// Exporta el reporte a un archivo Excel
    /// </summary>
    byte[] ExportarReporteExcel(ReporteMensualViewModel reporte);
    
    /// <summary>
    /// Exporta el reporte a un archivo PDF
    /// </summary>
    byte[] ExportarReportePdf(ReporteMensualViewModel reporte);
}



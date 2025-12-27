using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Globalization;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class ReportesController : Controller
{
    private readonly IReporteService _reporteService;
    private readonly CultureInfo _cultura = new CultureInfo("es-NI");

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // Por defecto, mostrar el mes actual
        var fechaActual = DateTime.Now;
        ViewBag.MesActual = fechaActual.Month;
        ViewBag.AñoActual = fechaActual.Year;
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GenerarReporte(string tipoFiltro, int? mes, int? año, 
                                        DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            Models.ViewModels.ReporteMensualViewModel reporte;

            if (tipoFiltro == "mes")
            {
                if (!mes.HasValue || !año.HasValue)
                {
                    TempData["Error"] = "Debe seleccionar un mes y año válidos.";
                    return RedirectToAction(nameof(Index));
                }

                reporte = _reporteService.GenerarReportePorMes(año.Value, mes.Value);
            }
            else if (tipoFiltro == "rango")
            {
                if (!fechaInicio.HasValue || !fechaFin.HasValue)
                {
                    TempData["Error"] = "Debe seleccionar un rango de fechas válido.";
                    return RedirectToAction(nameof(Index));
                }

                if (fechaInicio.Value > fechaFin.Value)
                {
                    TempData["Error"] = "La fecha de inicio debe ser anterior a la fecha final.";
                    return RedirectToAction(nameof(Index));
                }

                reporte = _reporteService.GenerarReporteMensual(fechaInicio.Value, fechaFin.Value);
            }
            else
            {
                TempData["Error"] = "Tipo de filtro no válido.";
                return RedirectToAction(nameof(Index));
            }

            // Pasar parámetros a la vista para los botones de exportación
            ViewBag.TipoFiltro = tipoFiltro;
            ViewBag.Mes = mes;
            ViewBag.Año = año;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            TempData["Success"] = "Reporte generado correctamente.";
            return View("Reporte", reporte);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar el reporte: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult ExportarExcel(string tipoFiltro, int? mes, int? año, DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            Models.ViewModels.ReporteMensualViewModel reporte;

            if (tipoFiltro == "mes" && mes.HasValue && año.HasValue)
            {
                reporte = _reporteService.GenerarReportePorMes(año.Value, mes.Value);
            }
            else if (tipoFiltro == "rango" && fechaInicio.HasValue && fechaFin.HasValue)
            {
                reporte = _reporteService.GenerarReporteMensual(fechaInicio.Value, fechaFin.Value);
            }
            else
            {
                TempData["Error"] = "Parámetros inválidos para exportar el reporte.";
                return RedirectToAction(nameof(Index));
            }

            var excelBytes = _reporteService.ExportarReporteExcel(reporte);
            var nombreArchivo = $"Reporte_{reporte.PeriodoTexto.Replace("/", "-").Replace(" ", "_")}.xlsx";
            
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar el reporte: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public IActionResult ExportarPdf(string tipoFiltro, int? mes, int? año, DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            Models.ViewModels.ReporteMensualViewModel reporte;

            if (tipoFiltro == "mes" && mes.HasValue && año.HasValue)
            {
                reporte = _reporteService.GenerarReportePorMes(año.Value, mes.Value);
            }
            else if (tipoFiltro == "rango" && fechaInicio.HasValue && fechaFin.HasValue)
            {
                reporte = _reporteService.GenerarReporteMensual(fechaInicio.Value, fechaFin.Value);
            }
            else
            {
                TempData["Error"] = "Parámetros inválidos para exportar el reporte.";
                return RedirectToAction(nameof(Index));
            }

            var pdfBytes = _reporteService.ExportarReportePdf(reporte);
            var nombreArchivo = $"Reporte_{reporte.PeriodoTexto.Replace("/", "-").Replace(" ", "_")}.pdf";
            
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar el reporte: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}



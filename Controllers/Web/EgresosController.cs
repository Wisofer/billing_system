using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class EgresosController : Controller
{
    private readonly IEgresoService _egresoService;

    public EgresosController(IEgresoService egresoService)
    {
        _egresoService = egresoService;
    }

    [HttpGet("/egresos")]
    public IActionResult Index(int pagina = 1, int tamanoPagina = 15, string? busqueda = null, string? categoria = null, string? fechaInicio = null, string? fechaFin = null)
    {
        DateTime? fechaInicioDate = null;
        DateTime? fechaFinDate = null;

        if (!string.IsNullOrWhiteSpace(fechaInicio) && DateTime.TryParse(fechaInicio, out var fi))
        {
            fechaInicioDate = fi;
        }
        if (!string.IsNullOrWhiteSpace(fechaFin) && DateTime.TryParse(fechaFin, out var ff))
        {
            fechaFinDate = ff;
        }

        var resultado = _egresoService.ObtenerPaginados(pagina, tamanoPagina, busqueda, categoria, fechaInicioDate, fechaFinDate);

        // Estadísticas
        var totalEgresos = _egresoService.CalcularTotalEgresos();
        var egresosMesActual = _egresoService.CalcularTotalEgresosMesActual();
        var egresosPorCategoria = _egresoService.ObtenerEgresosPorCategoria();

        ViewBag.TotalEgresos = totalEgresos;
        ViewBag.EgresosMesActual = egresosMesActual;
        ViewBag.EgresosPorCategoria = egresosPorCategoria;
        ViewBag.CantidadEgresos = _egresoService.ObtenerActivos().Count;

        // Parámetros de filtro
        ViewBag.Busqueda = busqueda;
        ViewBag.Categoria = categoria;
        ViewBag.FechaInicio = fechaInicio;
        ViewBag.FechaFin = fechaFin;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();

        return View(resultado);
    }

    [HttpGet("/egresos/crear")]
    public IActionResult Crear()
    {
        ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
        ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
        ViewBag.NuevoCodigo = _egresoService.GenerarCodigo();
        return View();
    }

    [HttpPost("/egresos/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(Egreso egreso)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(egreso.Descripcion))
        {
            ModelState.AddModelError("Descripcion", "La descripción es requerida");
        }
        if (string.IsNullOrWhiteSpace(egreso.Categoria))
        {
            ModelState.AddModelError("Categoria", "La categoría es requerida");
        }
        if (egreso.Monto <= 0)
        {
            ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
        }

        ModelState.Remove("Usuario");
        ModelState.Remove("Codigo");

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
            ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
            ViewBag.NuevoCodigo = _egresoService.GenerarCodigo();
            return View(egreso);
        }

        try
        {
            // Obtener usuario actual
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (int.TryParse(usuarioIdStr, out int usuarioId))
            {
                egreso.UsuarioId = usuarioId;
            }

            egreso.Codigo = _egresoService.GenerarCodigo();
            _egresoService.Crear(egreso);

            TempData["Exito"] = $"Egreso {egreso.Codigo} registrado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear el egreso: {ex.Message}";
            ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
            ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
            ViewBag.NuevoCodigo = _egresoService.GenerarCodigo();
            return View(egreso);
        }
    }

    [HttpGet("/egresos/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var egreso = _egresoService.ObtenerPorId(id);
        if (egreso == null)
        {
            TempData["Error"] = "Egreso no encontrado";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
        ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
        return View(egreso);
    }

    [HttpPost("/egresos/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, Egreso egreso)
    {
        if (id != egreso.Id)
        {
            TempData["Error"] = "ID de egreso no válido";
            return RedirectToAction(nameof(Index));
        }

        // Validaciones
        if (string.IsNullOrWhiteSpace(egreso.Descripcion))
        {
            ModelState.AddModelError("Descripcion", "La descripción es requerida");
        }
        if (string.IsNullOrWhiteSpace(egreso.Categoria))
        {
            ModelState.AddModelError("Categoria", "La categoría es requerida");
        }
        if (egreso.Monto <= 0)
        {
            ModelState.AddModelError("Monto", "El monto debe ser mayor a 0");
        }

        ModelState.Remove("Usuario");
        ModelState.Remove("Codigo");

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
            ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
            return View(egreso);
        }

        try
        {
            _egresoService.Actualizar(egreso);
            TempData["Exito"] = "Egreso actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar el egreso: {ex.Message}";
            ViewBag.Categorias = CategoriasEgreso.ObtenerTodas();
            ViewBag.MetodosPago = MetodosPagoEgreso.ObtenerTodos();
            return View(egreso);
        }
    }

    [HttpPost("/egresos/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var egreso = _egresoService.ObtenerPorId(id);
            if (egreso == null)
            {
                TempData["Error"] = "Egreso no encontrado";
                return RedirectToAction(nameof(Index));
            }

            _egresoService.Eliminar(id);
            TempData["Exito"] = $"Egreso {egreso.Codigo} eliminado exitosamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar el egreso: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/egresos/exportar-excel")]
    public IActionResult ExportarExcel(string? busqueda = null, string? categoria = null, string? fechaInicio = null, string? fechaFin = null)
    {
        DateTime? fechaInicioDate = null;
        DateTime? fechaFinDate = null;

        if (!string.IsNullOrWhiteSpace(fechaInicio) && DateTime.TryParse(fechaInicio, out var fi))
        {
            fechaInicioDate = fi;
        }
        if (!string.IsNullOrWhiteSpace(fechaFin) && DateTime.TryParse(fechaFin, out var ff))
        {
            fechaFinDate = ff;
        }

        // Obtener todos los egresos filtrados (sin paginación)
        var resultado = _egresoService.ObtenerPaginados(1, int.MaxValue, busqueda, categoria, fechaInicioDate, fechaFinDate);
        var egresos = resultado.Items;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Egresos");

        // Encabezados
        worksheet.Cells[1, 1].Value = "Código";
        worksheet.Cells[1, 2].Value = "Fecha";
        worksheet.Cells[1, 3].Value = "Descripción";
        worksheet.Cells[1, 4].Value = "Categoría";
        worksheet.Cells[1, 5].Value = "Proveedor";
        worksheet.Cells[1, 6].Value = "Método Pago";
        worksheet.Cells[1, 7].Value = "Monto";
        worksheet.Cells[1, 8].Value = "N° Factura";
        worksheet.Cells[1, 9].Value = "Observaciones";

        // Estilo encabezados
        using (var range = worksheet.Cells[1, 1, 1, 9])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(239, 68, 68));
            range.Style.Font.Color.SetColor(System.Drawing.Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // Datos
        int row = 2;
        decimal totalMonto = 0;
        foreach (var e in egresos)
        {
            worksheet.Cells[row, 1].Value = e.Codigo;
            worksheet.Cells[row, 2].Value = e.Fecha.ToString("dd/MM/yyyy");
            worksheet.Cells[row, 3].Value = e.Descripcion;
            worksheet.Cells[row, 4].Value = e.Categoria;
            worksheet.Cells[row, 5].Value = e.Proveedor ?? "-";
            worksheet.Cells[row, 6].Value = e.MetodoPago;
            worksheet.Cells[row, 7].Value = e.Monto;
            worksheet.Cells[row, 8].Value = e.NumeroFactura ?? "-";
            worksheet.Cells[row, 9].Value = e.Observaciones ?? "-";
            totalMonto += e.Monto;
            row++;
        }

        // Fila de total
        worksheet.Cells[row, 6].Value = "TOTAL:";
        worksheet.Cells[row, 6].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Value = totalMonto;
        worksheet.Cells[row, 7].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";

        // Formato moneda
        worksheet.Cells[2, 7, row, 7].Style.Numberformat.Format = "#,##0.00";

        // Ajustar ancho de columnas
        worksheet.Cells.AutoFitColumns();

        var stream = new MemoryStream();
        package.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Egresos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}


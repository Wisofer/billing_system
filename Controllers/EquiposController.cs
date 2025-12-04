using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class EquiposController : Controller
{
    private readonly IEquipoService _equipoService;
    private readonly ICategoriaEquipoService _categoriaEquipoService;
    private readonly IUbicacionService _ubicacionService;
    private readonly IProveedorService _proveedorService;

    public EquiposController(
        IEquipoService equipoService,
        ICategoriaEquipoService categoriaEquipoService,
        IUbicacionService ubicacionService,
        IProveedorService proveedorService)
    {
        _equipoService = equipoService;
        _categoriaEquipoService = categoriaEquipoService;
        _ubicacionService = ubicacionService;
        _proveedorService = proveedorService;
    }

    [HttpGet("/equipos")]
    public IActionResult Index(string? busqueda, string? estado, int? categoriaId, int? ubicacionId, int pagina = 1, int tamanoPagina = 25)
    {
        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;

        var resultado = _equipoService.ObtenerPaginados(pagina, tamanoPagina, busqueda, estado, categoriaId, ubicacionId);

        // Estadísticas
        var totalEquipos = _equipoService.ObtenerTotal();
        var disponibles = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoDisponible);
        var enUso = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoEnUso);
        var danados = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoDanado);
        var enReparacion = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoEnReparacion);
        var conStockMinimo = _equipoService.ObtenerConStockMinimo().Count;
        var valorTotal = _equipoService.ObtenerValorTotalInventario();

        ViewBag.Busqueda = busqueda;
        ViewBag.Estado = estado;
        ViewBag.CategoriaId = categoriaId;
        ViewBag.UbicacionId = ubicacionId;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = resultado.TotalItems;
        ViewBag.TotalEquipos = totalEquipos;
        ViewBag.Disponibles = disponibles;
        ViewBag.EnUso = enUso;
        ViewBag.Danados = danados;
        ViewBag.EnReparacion = enReparacion;
        ViewBag.ConStockMinimo = conStockMinimo;
        ViewBag.ValorTotal = valorTotal;

        // Listas para filtros
        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Estados = new[] { "Todos", SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
        ViewBag.EsAdministrador = SecurityHelper.IsAdministrator(User);

        return View(resultado);
    }

    [HttpGet("/equipos/ver/{id}")]
    public IActionResult Ver(int id)
    {
        var equipo = _equipoService.ObtenerPorId(id);
        if (equipo == null)
        {
            TempData["Error"] = "Equipo no encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(equipo);
    }

    [HttpGet("/equipos/crear")]
    public IActionResult Crear()
    {
        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Proveedores = _proveedorService.ObtenerActivos();
        ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };

        return View();
    }

    [HttpPost("/equipos/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(Equipo equipo)
    {
        // Quitar validaciones automáticas innecesarias (codigo y propiedades de navegación)
        // para que no marquen como requeridos campos que vamos a manejar manualmente
        ModelState.Remove("Codigo");
        ModelState.Remove("CategoriaEquipo");
        ModelState.Remove("Ubicacion");

        // Validaciones manuales adicionales
        if (string.IsNullOrWhiteSpace(equipo.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del equipo es requerido");
        }

        if (equipo.CategoriaEquipoId <= 0)
        {
            ModelState.AddModelError("CategoriaEquipoId", "Debes seleccionar una categoría");
        }

        if (equipo.UbicacionId <= 0)
        {
            ModelState.AddModelError("UbicacionId", "Debes seleccionar una ubicación");
        }

        if (equipo.Stock < 0)
        {
            ModelState.AddModelError("Stock", "El stock no puede ser negativo");
        }

        if (string.IsNullOrWhiteSpace(equipo.Estado))
        {
            equipo.Estado = SD.EstadoEquipoDisponible; // Valor por defecto
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }

        var equipoCreado = _equipoService.Crear(equipo);
        TempData["Success"] = $"✅ Equipo '{equipoCreado.Nombre}' (Código: {equipoCreado.Codigo}) creado exitosamente";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/equipos/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var equipo = _equipoService.ObtenerPorId(id);
        if (equipo == null)
        {
            TempData["Error"] = "Equipo no encontrado";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Proveedores = _proveedorService.ObtenerActivos();
        ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };

        return View(equipo);
    }

    [HttpPost("/equipos/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, Equipo equipo)
    {
        // Evitar errores de validación automática sobre propiedades de navegación
        // que no se envían en el formulario (solo se mandan los Ids).
        ModelState.Remove("CategoriaEquipo");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("Proveedor");

        if (id != equipo.Id)
        {
            TempData["Error"] = "ID de equipo no coincide";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }

        try
        {
            _equipoService.Actualizar(equipo);
            TempData["Success"] = $"Equipo '{equipo.Nombre}' actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }
    }

    [HttpPost("/equipos/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var equipo = _equipoService.ObtenerPorId(id);
            if (equipo == null)
            {
                TempData["Error"] = "Equipo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (_equipoService.Eliminar(id))
            {
                TempData["Success"] = $"Equipo '{equipo.Nombre}' eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se puede eliminar el equipo porque tiene asignaciones activas";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/equipos/exportar-excel")]
    public IActionResult ExportarExcel()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Obtener todos los equipos con sus relaciones
                var equipos = _equipoService.ObtenerTodos()
                    .OrderBy(e => e.Nombre)
                    .ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Equipos");

                    // Encabezados
                    worksheet.Cells[1, 1].Value = "Código";
                    worksheet.Cells[1, 2].Value = "Nombre";
                    worksheet.Cells[1, 3].Value = "Categoría";
                    worksheet.Cells[1, 4].Value = "Ubicación";
                    worksheet.Cells[1, 5].Value = "Marca";
                    worksheet.Cells[1, 6].Value = "Modelo";
                    worksheet.Cells[1, 7].Value = "Número de Serie";
                    worksheet.Cells[1, 8].Value = "Proveedor";
                    worksheet.Cells[1, 9].Value = "Stock";
                    worksheet.Cells[1, 10].Value = "Stock Mínimo";
                    worksheet.Cells[1, 11].Value = "Precio Compra";
                    worksheet.Cells[1, 12].Value = "Estado";
                    worksheet.Cells[1, 13].Value = "Fecha Adquisición";
                    worksheet.Cells[1, 14].Value = "Fecha Creación";

                    // Formato encabezados (simple, sin depender de System.Drawing)
                    using (var range = worksheet.Cells[1, 1, 1, 14])
                    {
                        range.Style.Font.Bold = true;
                    }

                    // Datos
                    int row = 2;
                    foreach (var e in equipos)
                    {
                        worksheet.Cells[row, 1].Value = e.Codigo;
                        worksheet.Cells[row, 2].Value = e.Nombre;
                        worksheet.Cells[row, 3].Value = e.CategoriaEquipo?.Nombre ?? "";
                        worksheet.Cells[row, 4].Value = e.Ubicacion?.Nombre ?? "";
                        worksheet.Cells[row, 5].Value = e.Marca ?? "";
                        worksheet.Cells[row, 6].Value = e.Modelo ?? "";
                        worksheet.Cells[row, 7].Value = e.NumeroSerie ?? "";
                        worksheet.Cells[row, 8].Value = e.Proveedor?.Nombre ?? "";
                        worksheet.Cells[row, 9].Value = e.Stock;
                        worksheet.Cells[row, 10].Value = e.StockMinimo;
                        worksheet.Cells[row, 11].Value = e.PrecioCompra;
                        worksheet.Cells[row, 12].Value = e.Estado;
                        worksheet.Cells[row, 13].Value = e.FechaAdquisicion?.ToString("dd/MM/yyyy") ?? "";
                        worksheet.Cells[row, 14].Value = e.FechaCreacion.ToString("dd/MM/yyyy HH:mm");

                        row++;
                    }

                    // Ajustar ancho de columnas básico
                    for (int col = 1; col <= 14; col++)
                    {
                        worksheet.Column(col).AutoFit();
                    }

                    var nombreArchivo = $"Equipos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(package.GetAsByteArray(), contentType, nombreArchivo);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar equipos a Excel: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
    }

    [HttpPost("/equipos/cambiar-estado")]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEstado(int equipoId, string nuevoEstado, string? motivo = null)
    {
        try
        {
            var usuarioId = SecurityHelper.GetUserId(User) ?? 0;
            if (_equipoService.CambiarEstado(equipoId, nuevoEstado, usuarioId, motivo))
            {
                TempData["Success"] = "Estado del equipo actualizado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el estado del equipo";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Ver), new { id = equipoId });
    }
}


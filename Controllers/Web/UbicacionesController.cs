using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class UbicacionesController : Controller
{
    private readonly IUbicacionService _ubicacionService;
    private readonly IEquipoService _equipoService;

    public UbicacionesController(
        IUbicacionService ubicacionService,
        IEquipoService equipoService)
    {
        _ubicacionService = ubicacionService;
        _equipoService = equipoService;
    }

    [HttpGet("/ubicaciones")]
    public IActionResult Index(string? busqueda, string? tipo)
    {
        var ubicaciones = _ubicacionService.ObtenerTodas();
        
        // Filtrar por búsqueda si existe
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.ToLower();
            ubicaciones = ubicaciones.Where(u => 
                u.Nombre.ToLower().Contains(termino) ||
                (u.Direccion != null && u.Direccion.ToLower().Contains(termino))
            ).ToList();
        }

        // Filtrar por tipo si existe
        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
        {
            ubicaciones = ubicaciones.Where(u => u.Tipo == tipo).ToList();
        }

        // Obtener cantidad de equipos por ubicación
        var todosEquipos = _equipoService.ObtenerTodos();
        var ubicacionesConConteo = ubicaciones.Select(u => new
        {
            Ubicacion = u,
            CantidadEquipos = todosEquipos.Count(e => e.UbicacionId == u.Id && e.Activo)
        }).OrderBy(x => x.Ubicacion.Nombre).ToList();

        ViewBag.Busqueda = busqueda;
        ViewBag.Tipo = tipo;
        ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
        ViewBag.UbicacionesConConteo = ubicacionesConConteo;
        
        return View(ubicaciones);
    }

    [HttpGet("/ubicaciones/crear")]
    public IActionResult Crear()
    {
        ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
        return View(new Ubicacion { Tipo = SD.TipoUbicacionAlmacen });
    }

    [HttpPost("/ubicaciones/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(Ubicacion ubicacion)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
            return View(ubicacion);
        }

        try
        {
            var ubicacionCreada = _ubicacionService.Crear(ubicacion);
            TempData["Success"] = $"✅ Ubicación '{ubicacionCreada.Nombre}' creada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
            return View(ubicacion);
        }
    }

    [HttpGet("/ubicaciones/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var ubicacion = _ubicacionService.ObtenerPorId(id);
        if (ubicacion == null)
        {
            TempData["Error"] = "Ubicación no encontrada";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
        return View(ubicacion);
    }

    [HttpPost("/ubicaciones/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, Ubicacion ubicacion)
    {
        if (id != ubicacion.Id)
        {
            TempData["Error"] = "ID de ubicación no coincide";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
            return View(ubicacion);
        }

        try
        {
            var ubicacionActualizada = _ubicacionService.Actualizar(ubicacion);
            TempData["Success"] = $"✅ Ubicación '{ubicacionActualizada.Nombre}' actualizada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Tipos = new[] { SD.TipoUbicacionAlmacen, SD.TipoUbicacionCampo, SD.TipoUbicacionReparacion };
            return View(ubicacion);
        }
    }

    [HttpPost("/ubicaciones/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var ubicacion = _ubicacionService.ObtenerPorId(id);
            if (ubicacion == null)
            {
                TempData["Error"] = "Ubicación no encontrada";
                return RedirectToAction(nameof(Index));
            }

            // Verificar si tiene equipos asociados
            var cantidadEquipos = _equipoService.ObtenerTodos().Count(e => e.UbicacionId == id && e.Activo);
            if (cantidadEquipos > 0)
            {
                TempData["Error"] = $"No se puede eliminar la ubicación '{ubicacion.Nombre}' porque tiene {cantidadEquipos} equipo(s) asociado(s). Desactívala en lugar de eliminarla.";
                return RedirectToAction(nameof(Index));
            }

            var eliminada = _ubicacionService.Eliminar(id);
            if (eliminada)
            {
                TempData["Success"] = $"✅ Ubicación '{ubicacion.Nombre}' eliminada exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar la ubicación";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/ubicaciones/toggle-activo/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivo(int id)
    {
        try
        {
            var ubicacion = _ubicacionService.ObtenerPorId(id);
            if (ubicacion == null)
            {
                TempData["Error"] = "Ubicación no encontrada";
                return RedirectToAction(nameof(Index));
            }

            ubicacion.Activo = !ubicacion.Activo;
            _ubicacionService.Actualizar(ubicacion);

            var estado = ubicacion.Activo ? "activada" : "desactivada";
            TempData["Success"] = $"✅ Ubicación '{ubicacion.Nombre}' {estado} exitosamente";
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}


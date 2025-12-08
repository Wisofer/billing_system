using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class ServiciosLandingPageController : Controller
{
    private readonly IServicioLandingPageService _servicioLandingPageService;

    public ServiciosLandingPageController(IServicioLandingPageService servicioLandingPageService)
    {
        _servicioLandingPageService = servicioLandingPageService;
    }

    [HttpGet("/servicios-landing")]
    public IActionResult Index()
    {
        var servicios = _servicioLandingPageService.ObtenerTodos();
        return View(servicios);
    }

    [HttpGet("/servicios-landing/crear")]
    public IActionResult Crear()
    {
        return View();
    }

    [HttpPost("/servicios-landing/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(ServicioLandingPage servicio)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(servicio.Titulo))
        {
            ModelState.AddModelError("Titulo", "El título es requerido.");
        }

        if (string.IsNullOrWhiteSpace(servicio.Descripcion))
        {
            ModelState.AddModelError("Descripcion", "La descripción es requerida.");
        }

        if (servicio.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser mayor a 0.");
        }

        if (!ModelState.IsValid)
        {
            return View(servicio);
        }

        try
        {
            var servicioCreado = _servicioLandingPageService.Crear(servicio);
            TempData["Success"] = $"✅ Servicio '{servicioCreado.Titulo}' creado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear servicio: {ex.Message}";
            return View(servicio);
        }
    }

    [HttpGet("/servicios-landing/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var servicio = _servicioLandingPageService.ObtenerPorId(id);
        if (servicio == null)
        {
            TempData["Error"] = "Servicio no encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(servicio);
    }

    [HttpPost("/servicios-landing/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, ServicioLandingPage servicio)
    {
        if (id != servicio.Id)
        {
            TempData["Error"] = "ID de servicio no coincide";
            return RedirectToAction(nameof(Index));
        }

        // Validaciones
        if (string.IsNullOrWhiteSpace(servicio.Titulo))
        {
            ModelState.AddModelError("Titulo", "El título es requerido.");
        }

        if (string.IsNullOrWhiteSpace(servicio.Descripcion))
        {
            ModelState.AddModelError("Descripcion", "La descripción es requerida.");
        }

        if (servicio.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser mayor a 0.");
        }

        if (!ModelState.IsValid)
        {
            return View(servicio);
        }

        try
        {
            if (_servicioLandingPageService.Actualizar(servicio))
            {
                TempData["Success"] = $"✅ Servicio '{servicio.Titulo}' actualizado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el servicio";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar servicio: {ex.Message}";
            return View(servicio);
        }
    }

    [HttpPost("/servicios-landing/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var servicio = _servicioLandingPageService.ObtenerPorId(id);
            if (servicio == null)
            {
                TempData["Error"] = "Servicio no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (_servicioLandingPageService.Eliminar(id))
            {
                TempData["Success"] = $"✅ Servicio '{servicio.Titulo}' eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el servicio";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar servicio: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/servicios-landing/toggle-activo/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivo(int id)
    {
        try
        {
            var servicio = _servicioLandingPageService.ObtenerPorId(id);
            if (servicio == null)
            {
                TempData["Error"] = "Servicio no encontrado";
                return RedirectToAction(nameof(Index));
            }

            servicio.Activo = !servicio.Activo;
            if (_servicioLandingPageService.Actualizar(servicio))
            {
                var estado = servicio.Activo ? "activado" : "desactivado";
                TempData["Success"] = $"✅ Servicio '{servicio.Titulo}' {estado} exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo cambiar el estado";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/servicios-landing/toggle-destacado/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleDestacado(int id)
    {
        try
        {
            var servicio = _servicioLandingPageService.ObtenerPorId(id);
            if (servicio == null)
            {
                TempData["Error"] = "Servicio no encontrado";
                return RedirectToAction(nameof(Index));
            }

            servicio.Destacado = !servicio.Destacado;
            if (_servicioLandingPageService.Actualizar(servicio))
            {
                var estado = servicio.Destacado ? "marcado como destacado" : "desmarcado como destacado";
                TempData["Success"] = $"✅ Servicio '{servicio.Titulo}' {estado} exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo cambiar el estado destacado";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cambiar estado destacado: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/servicios-landing/actualizar-orden")]
    [ValidateAntiForgeryToken]
    public IActionResult ActualizarOrden([FromBody] Dictionary<int, int> ordenPorId)
    {
        try
        {
            if (_servicioLandingPageService.ActualizarOrden(ordenPorId))
            {
                return Json(new { success = true, message = "Orden actualizado exitosamente" });
            }
            return Json(new { success = false, message = "No se pudo actualizar el orden" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}


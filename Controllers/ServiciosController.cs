using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.AspNetCore.Http;

namespace billing_system.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class ServiciosController : Controller
{
    private readonly IServicioService _servicioService;

    public ServiciosController(IServicioService servicioService)
    {
        _servicioService = servicioService;
    }

    [HttpGet("/servicios")]
    public IActionResult Index()
    {
        var servicios = _servicioService.ObtenerTodos();
        var esAdministrador = SecurityHelper.IsAdministrator(User);

        ViewBag.EsAdministrador = esAdministrador;
        return View(servicios);
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/servicios/crear")]
    public IActionResult Crear()
    {
        return View();
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/servicios/crear")]
    public IActionResult Crear([FromForm] Servicio servicio)
    {
        // Manejar el checkbox Activo
        servicio.Activo = FormHelper.GetCheckboxValue(Request.Form, "Activo");

        // Validaciones
        if (string.IsNullOrWhiteSpace(servicio.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es requerido.");
        }

        if (servicio.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser mayor a cero.");
        }

        if (!ModelState.IsValid)
        {
            return View(servicio);
        }

        try
        {
            _servicioService.Crear(servicio);
            TempData["Success"] = "Servicio creado exitosamente.";
            return Redirect("/servicios");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear servicio: {ex.Message}";
            return View(servicio);
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/servicios/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var servicio = _servicioService.ObtenerPorId(id);
        if (servicio == null)
        {
            TempData["Error"] = "Servicio no encontrado.";
            return Redirect("/servicios");
        }

        return View(servicio);
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/servicios/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, [FromForm] Servicio servicio)
    {
        servicio.Id = id;

        // Manejar el checkbox Activo
        servicio.Activo = FormHelper.GetCheckboxValue(Request.Form, "Activo");

        // Validaciones
        if (string.IsNullOrWhiteSpace(servicio.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre es requerido.");
        }

        if (servicio.Precio <= 0)
        {
            ModelState.AddModelError("Precio", "El precio debe ser mayor a cero.");
        }

        if (!ModelState.IsValid)
        {
            return View(servicio);
        }

        try
        {
            _servicioService.Actualizar(servicio);
            TempData["Success"] = "Servicio actualizado exitosamente.";
            return Redirect("/servicios");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar servicio: {ex.Message}";
            return View(servicio);
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/servicios/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var eliminado = _servicioService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Servicio eliminado exitosamente.";
            }
            else
            {
                TempData["Error"] = "No se puede eliminar el servicio porque tiene facturas asociadas.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar servicio: {ex.Message}";
        }

        return Redirect("/servicios");
    }
}

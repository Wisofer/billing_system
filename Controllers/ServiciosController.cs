using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

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
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var servicios = _servicioService.ObtenerTodos();
        var esAdministrador = Helpers.EsAdministrador(HttpContext.Session);

        ViewBag.EsAdministrador = esAdministrador;
        return View(servicios);
    }

    [HttpGet("/servicios/crear")]
    public IActionResult Crear()
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para crear servicios.";
            return Redirect("/servicios");
        }

        return View();
    }

    [HttpPost("/servicios/crear")]
    public IActionResult Crear([FromForm] Servicio servicio)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para crear servicios.";
            return Redirect("/servicios");
        }

        // Manejar el checkbox Activo
        if (Request.Form["Activo"].ToString() == "true")
        {
            servicio.Activo = true;
        }
        else
        {
            servicio.Activo = false;
        }

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

    [HttpGet("/servicios/editar/{id}")]
    public IActionResult Editar(int id)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para editar servicios.";
            return Redirect("/servicios");
        }

        var servicio = _servicioService.ObtenerPorId(id);
        if (servicio == null)
        {
            TempData["Error"] = "Servicio no encontrado.";
            return Redirect("/servicios");
        }

        return View(servicio);
    }

    [HttpPost("/servicios/editar/{id}")]
    public IActionResult Editar(int id, [FromForm] Servicio servicio)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para editar servicios.";
            return Redirect("/servicios");
        }

        servicio.Id = id;

        // Manejar el checkbox Activo
        // Cuando el checkbox está marcado, se envía "true" del checkbox y "false" del hidden
        // Cuando no está marcado, solo se envía "false" del hidden
        // Request.Form["Activo"] puede devolver múltiples valores, necesitamos verificar si contiene "true"
        var activoValues = Request.Form["Activo"];
        servicio.Activo = activoValues.Contains("true");

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

    [HttpPost("/servicios/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para eliminar servicios.";
            return Redirect("/servicios");
        }

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


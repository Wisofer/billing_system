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
    public IActionResult Index(string? categoria = null)
    {
        List<Servicio> servicios;
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            servicios = _servicioService.ObtenerPorCategoria(categoria);
        }
        else
        {
            servicios = _servicioService.ObtenerTodos();
        }
        
        var esAdministrador = SecurityHelper.IsAdministrator(User);

        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.Categoria = categoria;
        ViewBag.CategoriaSeleccionada = categoria ?? "Todos";
        return View(servicios);
    }

    [HttpGet("/servicios/internet")]
    public IActionResult Internet()
    {
        var servicios = _servicioService.ObtenerPorCategoria(SD.CategoriaInternet);
        var esAdministrador = SecurityHelper.IsAdministrator(User);

        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.Categoria = SD.CategoriaInternet;
        ViewBag.Titulo = "Servicios de Internet";
        return View("Index", servicios);
    }

    [HttpGet("/servicios/streaming")]
    public IActionResult Streaming()
    {
        var servicios = _servicioService.ObtenerPorCategoria(SD.CategoriaStreaming);
        var esAdministrador = SecurityHelper.IsAdministrator(User);

        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.Categoria = SD.CategoriaStreaming;
        ViewBag.Titulo = "Servicios de Streaming";
        return View("Index", servicios);
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/servicios/crear")]
    public IActionResult Crear(string? categoria = null)
    {
        ViewBag.Categoria = categoria ?? SD.CategoriaInternet;
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
            
            // Redirigir a la categoría del servicio creado
            if (servicio.Categoria == SD.CategoriaInternet)
            {
                return Redirect("/servicios/internet");
            }
            else if (servicio.Categoria == SD.CategoriaStreaming)
            {
                return Redirect("/servicios/streaming");
            }
            
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

        if (string.IsNullOrWhiteSpace(servicio.Categoria))
        {
            ModelState.AddModelError("Categoria", "La categoría es requerida.");
        }
        else if (servicio.Categoria != SD.CategoriaInternet && servicio.Categoria != SD.CategoriaStreaming)
        {
            ModelState.AddModelError("Categoria", "La categoría debe ser Internet o Streaming.");
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
            
            // Redirigir a la categoría del servicio actualizado
            if (servicio.Categoria == SD.CategoriaInternet)
            {
                return Redirect("/servicios/internet");
            }
            else if (servicio.Categoria == SD.CategoriaStreaming)
            {
                return Redirect("/servicios/streaming");
            }
            
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

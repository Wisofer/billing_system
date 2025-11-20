using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Route("[controller]/[action]")]
public class FacturasController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;

    public FacturasController(IFacturaService facturaService, IClienteService clienteService, IServicioService servicioService)
    {
        _facturaService = facturaService;
        _clienteService = clienteService;
        _servicioService = servicioService;
    }

    [HttpGet("/facturas")]
    public IActionResult Index(string? estado, int? mes, int? año)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var facturas = _facturaService.ObtenerTodas();

        if (!string.IsNullOrWhiteSpace(estado))
        {
            facturas = facturas.Where(f => f.Estado == estado).ToList();
        }

        if (mes.HasValue && año.HasValue)
        {
            var fechaFiltro = new DateTime(año.Value, mes.Value, 1);
            facturas = facturas.Where(f => f.MesFacturacion.Year == fechaFiltro.Year && f.MesFacturacion.Month == fechaFiltro.Month).ToList();
        }

        ViewBag.Estado = estado;
        ViewBag.Mes = mes;
        ViewBag.Año = año;
        ViewBag.Clientes = _clienteService.ObtenerTodos();
        ViewBag.Servicios = _servicioService.ObtenerActivos();

        return View(facturas);
    }

    [HttpGet("/facturas/crear")]
    public IActionResult Crear()
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        ViewBag.Servicios = _servicioService.ObtenerActivos();
        return View();
    }

    [HttpPost("/facturas/crear")]
    public IActionResult Crear(Factura factura)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }

        try
        {
            _facturaService.Crear(factura);
            TempData["Success"] = "Factura creada exitosamente.";
            return Redirect("/facturas");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear factura: {ex.Message}";
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }
    }

    [HttpPost("/facturas/generar-automaticas")]
    public IActionResult GenerarAutomaticas()
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            TempData["Error"] = "No tienes permisos para generar facturas automáticas.";
            return Redirect("/facturas");
        }

        try
        {
            _facturaService.GenerarFacturasAutomaticas();
            TempData["Success"] = "Facturas automáticas generadas exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar facturas: {ex.Message}";
        }

        return Redirect("/facturas");
    }

    [HttpPost("/facturas/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        try
        {
            var eliminado = _facturaService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Factura eliminada exitosamente.";
            }
            else
            {
                TempData["Error"] = "No se puede eliminar la factura porque tiene pagos asociados.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar factura: {ex.Message}";
        }

        return Redirect("/facturas");
    }
}

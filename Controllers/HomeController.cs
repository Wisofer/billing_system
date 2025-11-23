using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;

namespace billing_system.Controllers;

[Authorize]
[Route("[action]")]
public class HomeController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IPagoService _pagoService;
    private readonly IClienteService _clienteService;

    public HomeController(IFacturaService facturaService, IPagoService pagoService, IClienteService clienteService)
    {
        _facturaService = facturaService;
        _pagoService = pagoService;
        _clienteService = clienteService;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        // Optimizar: calcular valores una sola vez
        var pagosPendientes = _facturaService.CalcularTotalPendiente();
        var pagosRealizados = _pagoService.CalcularTotalIngresos();
        var totalClientes = _clienteService.ObtenerTotal();
        var totalFacturas = _facturaService.ObtenerTotal();
        var totalPagos = _pagoService.ObtenerTodos().Count;

        var viewModel = new DashboardViewModel
        {
            PagosPendientes = pagosPendientes,
            PagosRealizados = pagosRealizados,
            IngresoTotal = pagosRealizados,
            IngresoFaltante = pagosPendientes - pagosRealizados,
            TotalClientes = totalClientes,
            TotalFacturas = totalFacturas,
            TotalPagos = totalPagos
        };

        return View(viewModel);
    }

    [HttpGet("/error")]
    [AllowAnonymous]
    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode.HasValue)
        {
            ViewBag.StatusCode = statusCode.Value;
            ViewBag.ErrorMessage = statusCode.Value switch
            {
                404 => "Página no encontrada",
                403 => "Acceso denegado",
                500 => "Error interno del servidor",
                _ => "Ha ocurrido un error"
            };
        }
        else
        {
            ViewBag.StatusCode = 500;
            ViewBag.ErrorMessage = "Ha ocurrido un error";
        }

        return View();
    }
}


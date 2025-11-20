using Microsoft.AspNetCore.Mvc;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Data;

namespace billing_system.Controllers;

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
        // Si no está autenticado, redirigir al login
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var viewModel = new DashboardViewModel
        {
            PagosPendientes = _facturaService.CalcularTotalPendiente(),
            PagosRealizados = _pagoService.CalcularTotalIngresos(),
            IngresoTotal = _pagoService.CalcularTotalIngresos(),
            IngresoFaltante = _facturaService.CalcularTotalPendiente() - _pagoService.CalcularTotalIngresos(),
            TotalClientes = InMemoryStorage.Clientes.Count,
            TotalFacturas = InMemoryStorage.Facturas.Count,
            TotalPagos = InMemoryStorage.Pagos.Count
        };

        return View(viewModel);
    }
}


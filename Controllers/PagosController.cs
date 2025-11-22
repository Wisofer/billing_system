using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class PagosController : Controller
{
    private readonly IPagoService _pagoService;
    private readonly IFacturaService _facturaService;
    private readonly IClienteService _clienteService;

    public PagosController(IPagoService pagoService, IFacturaService facturaService, IClienteService clienteService)
    {
        _pagoService = pagoService;
        _facturaService = facturaService;
        _clienteService = clienteService;
    }

    [HttpGet("/pagos")]
    public IActionResult Index()
    {

        var pagos = _pagoService.ObtenerTodos();
        return View(pagos);
    }

    [HttpGet("/pagos/crear")]
    public IActionResult Crear(string? clienteBusqueda)
    {

        ViewBag.ClienteBusqueda = clienteBusqueda;
        ViewBag.Clientes = string.IsNullOrWhiteSpace(clienteBusqueda)
            ? new List<Cliente>()
            : _clienteService.Buscar(clienteBusqueda);

        ViewBag.Facturas = new List<Factura>();
        ViewBag.TipoCambio = SD.TipoCambioDolar;
        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
        ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };

        return View();
    }

    [HttpPost("/pagos/buscar-cliente")]
    public IActionResult BuscarCliente(string clienteBusqueda)
    {
        return RedirectToAction("Crear", new { clienteBusqueda });
    }

    [HttpGet("/pagos/facturas-cliente/{clienteId}")]
    public IActionResult FacturasCliente(int clienteId)
    {
        var facturas = _facturaService.ObtenerPorCliente(clienteId)
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .ToList();
        return Json(facturas.Select(f => new { f.Id, f.Numero, f.Monto, f.MesFacturacion }));
    }

    [HttpPost("/pagos/crear")]
    public IActionResult Crear(Pago pago)
    {

        if (!ModelState.IsValid)
        {
            ViewBag.TipoCambio = SD.TipoCambioDolar;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            return View(pago);
        }

        try
        {
            _pagoService.Crear(pago);
            TempData["Success"] = "Pago registrado exitosamente.";
            return Redirect("/pagos");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al registrar pago: {ex.Message}";
            ViewBag.TipoCambio = SD.TipoCambioDolar;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            return View(pago);
        }
    }

    [HttpPost("/pagos/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {

        try
        {
            var eliminado = _pagoService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Pago eliminado exitosamente.";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el pago.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar pago: {ex.Message}";
        }

        return Redirect("/pagos");
    }
}

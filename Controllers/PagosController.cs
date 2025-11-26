using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace billing_system.Controllers;

[Authorize(Policy = "Pagos")]
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
    public IActionResult Index(string? tipoPago, string? banco, DateTime? fecha)
    {
        var esAdministrador = SecurityHelper.IsAdministrator(User);
        
        var pagos = _pagoService.ObtenerTodos().AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(tipoPago) && tipoPago != "Todos")
        {
            pagos = pagos.Where(p => p.TipoPago == tipoPago);
        }

        if (!string.IsNullOrWhiteSpace(banco) && banco != "Todos")
        {
            pagos = pagos.Where(p => p.Banco == banco);
        }

        if (fecha.HasValue)
        {
            pagos = pagos.Where(p => p.FechaPago.Date == fecha.Value.Date);
        }

        var pagosLista = pagos.ToList();

        // Estadísticas (optimizado: calcular una sola vez)
        var todosPagos = _pagoService.ObtenerTodos();
        var totalPagos = todosPagos.Count;
        var montoTotal = _pagoService.CalcularTotalIngresos();
        var pagosFisicos = todosPagos.Count(p => p.TipoPago == SD.TipoPagoFisico);
        var pagosElectronicos = todosPagos.Count(p => p.TipoPago == SD.TipoPagoElectronico);

        ViewBag.TotalPagos = totalPagos;
        ViewBag.MontoTotal = montoTotal;
        ViewBag.PagosFisicos = pagosFisicos;
        ViewBag.PagosElectronicos = pagosElectronicos;
        ViewBag.TipoPago = tipoPago ?? "Todos";
        ViewBag.Banco = banco ?? "Todos";
        ViewBag.Fecha = fecha;
        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };

        return View(pagosLista);
    }

    [HttpGet("/pagos/crear")]
    public IActionResult Crear(string? clienteBusqueda)
    {
        ViewBag.ClienteBusqueda = clienteBusqueda;
        // Cargar todos los clientes para el buscador
        ViewBag.TodosLosClientes = _clienteService.ObtenerTodos();
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
        // Obtener todas las facturas del cliente (pendientes y pagadas)
        // Esto permite registrar pagos adicionales si es necesario
        var facturas = _facturaService.ObtenerPorCliente(clienteId)
            .OrderByDescending(f => f.MesFacturacion)
            .ThenByDescending(f => f.FechaCreacion)
            .ToList();
        
        // Calcular el total pagado para cada factura
        var facturasConPago = facturas.Select(f => 
        {
            var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
            var saldoPendiente = f.Monto - totalPagado;
            
            // Si el saldo pendiente es negativo o cero, no se puede pagar
            // Pero si es negativo, significa que se pagó de más, así que el saldo pendiente debe ser 0
            var saldoPendienteAjustado = saldoPendiente < 0 ? 0 : saldoPendiente;
            
            return new 
            { 
                f.Id, 
                f.Numero, 
                Monto = f.Monto, // Asegurar que sea un número, no string formateado
                MesFacturacion = f.MesFacturacion.ToString("yyyy-MM-dd"),
                f.Estado,
                TotalPagado = totalPagado, // Asegurar que sea un número
                SaldoPendiente = saldoPendienteAjustado, // Usar el saldo ajustado
                PuedePagar = saldoPendienteAjustado > 0 // Solo se puede pagar si hay saldo pendiente
            };
        }).ToList();
        
        return Json(facturasConPago);
    }

    [HttpPost("/pagos/crear")]
    public IActionResult Crear([FromForm] Pago pago)
    {
        // Log para debugging (remover en producción)
        var logger = HttpContext.RequestServices.GetService<ILogger<PagosController>>();
        
        // Obtener el valor crudo del formulario para debugging
        var montoRaw = Request.Form["Monto"].ToString();
        logger?.LogInformation($"=== DEBUG PAGO ===");
        logger?.LogInformation($"Monto crudo del formulario: '{montoRaw}'");
        logger?.LogInformation($"Monto parseado: {pago.Monto}");
        logger?.LogInformation($"FacturaId: {pago.FacturaId}, TipoPago: {pago.TipoPago}");

        // Remover errores de validación de propiedades de navegación (relaciones)
        ModelState.Remove("Factura");
        
        // Validación manual de campos requeridos
        if (pago.FacturaId == 0)
        {
            ModelState.AddModelError("FacturaId", "Debe seleccionar una factura");
        }

        if (string.IsNullOrWhiteSpace(pago.TipoPago))
        {
            ModelState.AddModelError("TipoPago", "Debe seleccionar un tipo de pago");
        }

        if (pago.Monto <= 0)
        {
            ModelState.AddModelError("Monto", "El monto debe ser mayor a cero");
        }

        // VALIDACIÓN CRÍTICA: Verificar que el monto no sea excesivamente alto
        var factura = _facturaService.ObtenerPorId(pago.FacturaId);
        if (factura != null)
        {
            // Obtener el saldo pendiente real
            var totalPagado = _pagoService.ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
            var saldoPendiente = factura.Monto - totalPagado;
            var saldoPendienteAjustado = saldoPendiente < 0 ? 0 : saldoPendiente;
            
            logger?.LogInformation($"Factura: {factura.Numero}, Monto Factura: {factura.Monto}, Total Pagado: {totalPagado}, Saldo Pendiente: {saldoPendienteAjustado}, Monto Pago: {pago.Monto}");
            
            // Validar que el monto del pago no exceda el saldo pendiente por más del 10%
            // Esto previene errores de entrada (como multiplicar por 100)
            if (pago.Monto > saldoPendienteAjustado * 1.1m)
            {
                ModelState.AddModelError("Monto", $"El monto del pago (C$ {pago.Monto:N2}) excede significativamente el saldo pendiente (C$ {saldoPendienteAjustado:N2}). Por favor, verifica el monto.");
                logger?.LogWarning($"Monto de pago sospechoso: {pago.Monto} vs saldo pendiente: {saldoPendienteAjustado}");
            }
            
            // Validar que el monto no sea más de 10 veces el monto de la factura (prevenir errores de multiplicación)
            if (pago.Monto > factura.Monto * 10m)
            {
                ModelState.AddModelError("Monto", $"El monto del pago (C$ {pago.Monto:N2}) es demasiado alto comparado con el monto de la factura (C$ {factura.Monto:N2}). Por favor, verifica el monto.");
                logger?.LogWarning($"Monto de pago excesivo: {pago.Monto} vs monto factura: {factura.Monto}");
            }
        }

        if (string.IsNullOrWhiteSpace(pago.Moneda))
        {
            ModelState.AddModelError("Moneda", "Debe seleccionar una moneda");
        }

        if (pago.TipoPago == SD.TipoPagoElectronico)
        {
            if (string.IsNullOrWhiteSpace(pago.Banco))
            {
                ModelState.AddModelError("Banco", "El banco es requerido para pagos electrónicos");
            }
            if (string.IsNullOrWhiteSpace(pago.TipoCuenta))
            {
                ModelState.AddModelError("TipoCuenta", "El tipo de cuenta es requerido para pagos electrónicos");
            }
        }

        // Validar solo los campos que realmente importan
        var isValid = ModelState.IsValid && 
                     pago.FacturaId > 0 && 
                     pago.Monto > 0 && 
                     !string.IsNullOrWhiteSpace(pago.TipoPago) &&
                     !string.IsNullOrWhiteSpace(pago.Moneda) &&
                     (pago.TipoPago != SD.TipoPagoElectronico || (!string.IsNullOrWhiteSpace(pago.Banco) && !string.IsNullOrWhiteSpace(pago.TipoCuenta)));

        if (!isValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !e.Contains("Factura")) // Filtrar errores de la relación Factura
                .ToList();
            
            logger?.LogWarning($"Errores de validación: {string.Join(", ", errors)}");
            
            ViewBag.TipoCambio = SD.TipoCambioDolar;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            ViewBag.ClienteBusqueda = "";
            ViewBag.Clientes = new List<Cliente>();
            ViewBag.Facturas = new List<Factura>();
            
            // Mostrar errores de validación (sin el error de Factura)
            if (errors.Any())
            {
                TempData["Error"] = $"Errores de validación: {string.Join(", ", errors)}";
            }
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
            logger?.LogError(ex, "Error al crear pago");
            TempData["Error"] = $"Error al registrar pago: {ex.Message}";
            ViewBag.TipoCambio = SD.TipoCambioDolar;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            ViewBag.ClienteBusqueda = "";
            ViewBag.Clientes = new List<Cliente>();
            ViewBag.Facturas = new List<Factura>();
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

    [Authorize(Policy = "Administrador")]
    [HttpPost("/pagos/eliminar-multiples")]
    public IActionResult EliminarMultiples([FromForm] List<int> pagoIds)
    {
        if (pagoIds == null || !pagoIds.Any())
        {
            TempData["Error"] = "No se seleccionaron pagos para eliminar.";
            return Redirect("/pagos");
        }

        try
        {
            var resultado = _pagoService.EliminarMultiples(pagoIds);
            
            var mensajes = new List<string>();
            if (resultado.eliminados > 0)
            {
                mensajes.Add($"{resultado.eliminados} pago(s) eliminado(s) exitosamente.");
            }
            if (resultado.noEncontrados > 0)
            {
                mensajes.Add($"{resultado.noEncontrados} pago(s) no encontrado(s).");
            }

            if (mensajes.Any())
            {
                if (resultado.eliminados > 0)
                {
                    TempData["Success"] = string.Join(" ", mensajes);
                }
                else
                {
                    TempData["Error"] = string.Join(" ", mensajes);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar pagos: {ex.Message}";
        }

        return Redirect("/pagos");
    }
}

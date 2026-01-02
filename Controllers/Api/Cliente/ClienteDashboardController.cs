using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Security.Claims;

namespace billing_system.Controllers.Api.Cliente
{
    [Route("api/cliente/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class DashboardController : ControllerBase
    {
        private readonly IFacturaService _facturaService;
        private readonly IPagoService _pagoService;

        public DashboardController(IFacturaService facturaService, IPagoService pagoService)
        {
            _facturaService = facturaService;
            _pagoService = pagoService;
        }

        /// <summary>
        /// Obtener resumen/dashboard del cliente - GET /api/cliente/dashboard
        /// </summary>
        [HttpGet]
        public IActionResult GetDashboard()
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                // Obtener facturas del cliente
                var facturas = _facturaService.ObtenerPorCliente(clienteId.Value);
                var facturasPendientes = facturas.Where(f => f.Estado == SD.EstadoFacturaPendiente).ToList();

                // Calcular saldo pendiente
                var saldoPendiente = 0m;
                foreach (var factura in facturasPendientes)
                {
                    var totalPagado = _pagoService.ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
                    var saldo = factura.Monto - totalPagado;
                    if (saldo > 0)
                        saldoPendiente += saldo;
                }

                // Obtener última factura
                var ultimaFactura = facturas.OrderByDescending(f => f.FechaCreacion).FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        saldoPendiente = saldoPendiente,
                        facturasPendientes = new
                        {
                            cantidad = facturasPendientes.Count,
                            monto = saldoPendiente
                        },
                        ultimaFactura = ultimaFactura != null ? new
                        {
                            id = ultimaFactura.Id,
                            numero = ultimaFactura.Numero,
                            monto = ultimaFactura.Monto,
                            estado = ultimaFactura.Estado,
                            fechaCreacion = ultimaFactura.FechaCreacion,
                            mesFacturacion = ultimaFactura.MesFacturacion
                        } : null,
                        resumen = new
                        {
                            totalFacturas = facturas.Count,
                            facturasPagadas = facturas.Count(f => f.Estado == SD.EstadoFacturaPagada),
                            facturasCanceladas = facturas.Count(f => f.Estado == SD.EstadoFacturaCancelada)
                        },
                        fecha = DateTime.Now
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener dashboard" });
            }
        }

        private int? ObtenerClienteId()
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(clienteIdClaim, out var clienteId))
            {
                return clienteId;
            }
            return null;
        }
    }
}



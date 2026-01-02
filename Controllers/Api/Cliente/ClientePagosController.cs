using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using System.Security.Claims;

namespace billing_system.Controllers.Api.Cliente
{
    [Route("api/cliente/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PagosController : ControllerBase
    {
        private readonly IPagoService _pagoService;
        private readonly IFacturaService _facturaService;

        public PagosController(IPagoService pagoService, IFacturaService facturaService)
        {
            _pagoService = pagoService;
            _facturaService = facturaService;
        }

        /// <summary>
        /// Obtener todos los pagos del cliente - GET /api/cliente/pagos
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20)
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                // Obtener todas las facturas del cliente
                var facturasDelCliente = _facturaService.ObtenerPorCliente(clienteId.Value);
                var facturasIds = facturasDelCliente.Select(f => f.Id).ToList();

                // Obtener todos los pagos relacionados con las facturas del cliente
                var todosPagos = _pagoService.ObtenerTodos();
                var pagos = todosPagos.Where(p =>
                    (p.FacturaId.HasValue && facturasIds.Contains(p.FacturaId.Value)) ||
                    (p.PagoFacturas != null && p.PagoFacturas.Any(pf => facturasIds.Contains(pf.FacturaId)))
                ).ToList();

                // Filtrar por fecha
                if (fechaInicio.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago >= fechaInicio.Value).ToList();
                }
                if (fechaFin.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago <= fechaFin.Value).ToList();
                }

                // Ordenar
                pagos = pagos.OrderByDescending(p => p.FechaPago).ToList();

                // Paginación
                var totalItems = pagos.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / tamanoPagina);
                var items = pagos.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

                return Ok(new
                {
                    success = true,
                    data = items.Select(p => new
                    {
                        id = p.Id,
                        monto = p.Monto,
                        moneda = p.Moneda,
                        tipoPago = p.TipoPago,
                        fechaPago = p.FechaPago,
                        facturas = p.FacturaId.HasValue
                            ? new[] { new { id = p.Factura?.Id, numero = p.Factura?.Numero } }
                            : (p.PagoFacturas?.Select(pf => new { id = pf.Factura?.Id, numero = pf.Factura?.Numero }).ToArray() ?? Array.Empty<object>())
                    }),
                    pagination = new
                    {
                        currentPage = pagina,
                        totalPages = totalPages,
                        totalItems = totalItems,
                        pageSize = tamanoPagina
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener pagos" });
            }
        }

        /// <summary>
        /// Obtener detalle de un pago - GET /api/cliente/pagos/{id}
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                var pago = _pagoService.ObtenerPorId(id);

                if (pago == null)
                {
                    return NotFound(new { success = false, message = "Pago no encontrado" });
                }

                // Verificar que el pago pertenece a una factura del cliente
                var facturasDelCliente = _facturaService.ObtenerPorCliente(clienteId.Value);
                var facturasIds = facturasDelCliente.Select(f => f.Id).ToList();

                bool perteneceAlCliente = false;
                if (pago.FacturaId.HasValue && facturasIds.Contains(pago.FacturaId.Value))
                {
                    perteneceAlCliente = true;
                }
                else if (pago.PagoFacturas != null && pago.PagoFacturas.Any(pf => facturasIds.Contains(pf.FacturaId)))
                {
                    perteneceAlCliente = true;
                }

                if (!perteneceAlCliente)
                {
                    return Forbid();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = pago.Id,
                        monto = pago.Monto,
                        moneda = pago.Moneda,
                        tipoPago = pago.TipoPago,
                        banco = pago.Banco,
                        fechaPago = pago.FechaPago,
                        observaciones = pago.Observaciones,
                        factura = pago.Factura != null ? new
                        {
                            id = pago.Factura.Id,
                            numero = pago.Factura.Numero,
                            monto = pago.Factura.Monto
                        } : null,
                        facturas = pago.PagoFacturas?.Select(pf => new
                        {
                            id = pf.Factura?.Id,
                            numero = pf.Factura?.Numero,
                            monto = pf.MontoAplicado
                        }).ToList()
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener pago" });
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


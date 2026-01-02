using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Services;
using billing_system.Utils;
using System.Security.Claims;

namespace billing_system.Controllers.Api.Cliente
{
    [Route("api/cliente/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FacturasController : ControllerBase
    {
        private readonly IFacturaService _facturaService;
        private readonly IPagoService _pagoService;
        private readonly IPdfService _pdfService;

        public FacturasController(
            IFacturaService facturaService,
            IPagoService pagoService,
            IPdfService pdfService)
        {
            _facturaService = facturaService;
            _pagoService = pagoService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Obtener todas las facturas del cliente - GET /api/cliente/facturas
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] string? estado = null,
            [FromQuery] string? categoria = null,
            [FromQuery] int? mes = null,
            [FromQuery] int? anio = null,
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

                var facturas = _facturaService.ObtenerPorCliente(clienteId.Value);

                // Filtros
                if (!string.IsNullOrWhiteSpace(estado) && estado != "Todas")
                {
                    facturas = facturas.Where(f => f.Estado == estado).ToList();
                }
                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    facturas = facturas.Where(f => f.Categoria == categoria).ToList();
                }
                if (mes.HasValue && anio.HasValue)
                {
                    facturas = facturas.Where(f => f.MesFacturacion.Month == mes && f.MesFacturacion.Year == anio).ToList();
                }

                // Ordenar
                facturas = facturas.OrderByDescending(f => f.FechaCreacion).ToList();

                // Paginación
                var totalItems = facturas.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / tamanoPagina);
                var items = facturas.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

                return Ok(new
                {
                    success = true,
                    data = items.Select(f =>
                    {
                        var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
                        var saldoPendiente = f.Monto - totalPagado;

                        return new
                        {
                            id = f.Id,
                            numero = f.Numero,
                            monto = f.Monto,
                            estado = f.Estado,
                            categoria = f.Categoria,
                            fechaCreacion = f.FechaCreacion,
                            mesFacturacion = f.MesFacturacion,
                            saldoPendiente = saldoPendiente > 0 ? saldoPendiente : 0,
                            totalPagado = totalPagado
                        };
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
                return StatusCode(500, new { success = false, message = "Error al obtener facturas" });
            }
        }

        /// <summary>
        /// Obtener detalle de una factura - GET /api/cliente/facturas/{id}
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

                var factura = _facturaService.ObtenerPorId(id);

                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                // Verificar que la factura pertenece al cliente autenticado
                if (factura.ClienteId != clienteId.Value)
                {
                    return Forbid();
                }

                var totalPagado = _pagoService.ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
                var saldoPendiente = factura.Monto - totalPagado;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = factura.Id,
                        numero = factura.Numero,
                        monto = factura.Monto,
                        estado = factura.Estado,
                        categoria = factura.Categoria,
                        fechaCreacion = factura.FechaCreacion,
                        mesFacturacion = factura.MesFacturacion,
                        saldoPendiente = saldoPendiente > 0 ? saldoPendiente : 0,
                        totalPagado = totalPagado,
                        servicios = factura.FacturaServicios?.Select(fs => new
                        {
                            id = fs.Servicio?.Id,
                            nombre = fs.Servicio?.Nombre,
                            descripcion = fs.Servicio?.Descripcion,
                            precio = fs.Servicio?.Precio,
                            cantidad = fs.Cantidad
                        }).ToList(),
                        pagos = factura.Pagos?.Select(p => new
                        {
                            id = p.Id,
                            monto = p.Monto,
                            fechaPago = p.FechaPago,
                            tipoPago = p.TipoPago,
                            moneda = p.Moneda
                        }).ToList()
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener factura" });
            }
        }

        /// <summary>
        /// Obtener facturas pendientes - GET /api/cliente/facturas/pendientes
        /// </summary>
        [HttpGet("pendientes")]
        public IActionResult GetPendientes()
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                var facturas = _facturaService.ObtenerPorCliente(clienteId.Value)
                    .Where(f => f.Estado == SD.EstadoFacturaPendiente)
                    .OrderByDescending(f => f.FechaCreacion)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = facturas.Select(f =>
                    {
                        var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
                        var saldoPendiente = f.Monto - totalPagado;

                        return new
                        {
                            id = f.Id,
                            numero = f.Numero,
                            monto = f.Monto,
                            saldoPendiente = saldoPendiente > 0 ? saldoPendiente : 0,
                            fechaCreacion = f.FechaCreacion,
                            mesFacturacion = f.MesFacturacion,
                            categoria = f.Categoria
                        };
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener facturas pendientes" });
            }
        }

        /// <summary>
        /// Descargar PDF de factura - GET /api/cliente/facturas/{id}/pdf
        /// </summary>
        [HttpGet("{id}/pdf")]
        public IActionResult DownloadPdf(int id)
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                var factura = _facturaService.ObtenerPorId(id);

                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                // Verificar que la factura pertenece al cliente autenticado
                if (factura.ClienteId != clienteId.Value)
                {
                    return Forbid();
                }

                var pdfBytes = _pdfService.GenerarPdfFactura(factura);

                return File(pdfBytes, "application/pdf", $"Factura_{factura.Numero}.pdf");
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al generar PDF" });
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


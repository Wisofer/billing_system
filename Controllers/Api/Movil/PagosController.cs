using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Models.Entities;
using billing_system.Utils;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PagosController : ControllerBase
    {
        private readonly IPagoService _pagoService;
        private readonly IFacturaService _facturaService;
        private readonly IConfiguracionService _configuracionService;

        public PagosController(
            IPagoService pagoService, 
            IFacturaService facturaService,
            IConfiguracionService configuracionService)
        {
            _pagoService = pagoService;
            _facturaService = facturaService;
            _configuracionService = configuracionService;
        }

        /// <summary>
        /// Obtener todos los pagos - GET /api/movil/pagos
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var pagos = _pagoService.ObtenerTodos();

                // Filtrar por fecha si se especifica
                if (fechaInicio.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago >= fechaInicio.Value).ToList();
                }
                if (fechaFin.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago <= fechaFin.Value).ToList();
                }

                // Ordenar por fecha descendente
                pagos = pagos.OrderByDescending(p => p.FechaPago).ToList();

                // Paginación manual
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
                        banco = p.Banco,
                        fechaPago = p.FechaPago,
                        factura = p.Factura != null ? new
                        {
                            id = p.Factura.Id,
                            numero = p.Factura.Numero,
                            cliente = p.Factura.Cliente?.Nombre
                        } : null
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
        /// Obtener un pago por ID - GET /api/movil/pagos/{id}
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var pago = _pagoService.ObtenerPorId(id);

                if (pago == null)
                {
                    return NotFound(new { success = false, message = "Pago no encontrado" });
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
                        tipoCuenta = pago.TipoCuenta,
                        fechaPago = pago.FechaPago,
                        montoRecibido = pago.MontoRecibido,
                        vuelto = pago.Vuelto,
                        tipoCambio = pago.TipoCambio,
                        observaciones = pago.Observaciones,
                        factura = pago.Factura != null ? new
                        {
                            id = pago.Factura.Id,
                            numero = pago.Factura.Numero,
                            monto = pago.Factura.Monto,
                            cliente = new
                            {
                                id = pago.Factura.Cliente?.Id,
                                nombre = pago.Factura.Cliente?.Nombre,
                                telefono = pago.Factura.Cliente?.Telefono
                            }
                        } : null
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener pago" });
            }
        }

        /// <summary>
        /// Obtener tipo de cambio actual - GET /api/movil/pagos/tipo-cambio
        /// </summary>
        [HttpGet("tipo-cambio")]
        public IActionResult GetTipoCambio()
        {
            try
            {
                var tipoCambioStr = _configuracionService.ObtenerValor("TipoCambioDolar");
                var tipoCambio = decimal.TryParse(tipoCambioStr, out var tc) ? tc : SD.TipoCambioDolar;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        tipoCambio = tipoCambio,
                        monedaBase = "USD",
                        monedaDestino = "NIO"
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener tipo de cambio" });
            }
        }

        /// <summary>
        /// Crear un nuevo pago (factura individual) - POST /api/movil/pagos
        /// </summary>
        [HttpPost]
        public IActionResult Create([FromBody] PagoCreateRequest request)
        {
            try
            {
                if (request.FacturaId <= 0)
                {
                    return BadRequest(new { success = false, message = "La factura es requerida" });
                }

                var factura = _facturaService.ObtenerPorId(request.FacturaId);
                
                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                if (factura.Estado == SD.EstadoFacturaPagada)
                {
                    return BadRequest(new { success = false, message = "La factura ya está pagada" });
                }

                var pago = new Pago
                {
                    FacturaId = request.FacturaId,
                    Monto = request.Monto > 0 ? request.Monto : factura.Monto,
                    Moneda = request.Moneda ?? "NIO",
                    TipoPago = request.TipoPago ?? "Efectivo",
                    Banco = request.Banco,
                    TipoCuenta = request.TipoCuenta,
                    MontoRecibido = request.MontoRecibido,
                    Vuelto = request.Vuelto,
                    TipoCambio = request.TipoCambio,
                    Observaciones = request.Observaciones,
                    FechaPago = DateTime.Now
                };

                var pagoCreado = _pagoService.Crear(pago);

                return CreatedAtAction(nameof(GetById), new { id = pagoCreado.Id }, new
                {
                    success = true,
                    message = "Pago registrado exitosamente",
                    data = new
                    {
                        id = pagoCreado.Id,
                        monto = pagoCreado.Monto,
                        fechaPago = pagoCreado.FechaPago
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al registrar pago" });
            }
        }

        /// <summary>
        /// Crear pago para múltiples facturas - POST /api/movil/pagos/multiples
        /// </summary>
        [HttpPost("multiples")]
        public IActionResult CreateMultiple([FromBody] PagoMultipleRequest request)
        {
            try
            {
                if (request.FacturaIds == null || !request.FacturaIds.Any())
                {
                    return BadRequest(new { success = false, message = "Debe seleccionar al menos una factura" });
                }

                var pago = new Pago
                {
                    Monto = request.MontoTotal,
                    Moneda = request.Moneda ?? "NIO",
                    TipoPago = request.TipoPago ?? "Efectivo",
                    Banco = request.Banco,
                    TipoCuenta = request.TipoCuenta,
                    MontoRecibido = request.MontoRecibido,
                    Vuelto = request.Vuelto,
                    TipoCambio = request.TipoCambio,
                    Observaciones = request.Observaciones,
                    FechaPago = DateTime.Now
                };

                var pagoCreado = _pagoService.Crear(pago, request.FacturaIds);

                return Ok(new
                {
                    success = true,
                    message = $"Pago registrado para {request.FacturaIds.Count} facturas",
                    data = new
                    {
                        id = pagoCreado.Id,
                        monto = pagoCreado.Monto,
                        cantidadFacturas = request.FacturaIds.Count
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al registrar pago múltiple" });
            }
        }

        /// <summary>
        /// Eliminar un pago - DELETE /api/movil/pagos/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var pago = _pagoService.ObtenerPorId(id);
                
                if (pago == null)
                {
                    return NotFound(new { success = false, message = "Pago no encontrado" });
                }

                _pagoService.Eliminar(id);

                return Ok(new
                {
                    success = true,
                    message = "Pago eliminado exitosamente"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al eliminar pago" });
            }
        }

        /// <summary>
        /// Obtener resumen de pagos del día - GET /api/movil/pagos/resumen-dia
        /// </summary>
        [HttpGet("resumen-dia")]
        public IActionResult GetResumenDia([FromQuery] DateTime? fecha = null)
        {
            try
            {
                var fechaConsulta = fecha ?? DateTime.Today;
                var pagos = _pagoService.ObtenerTodos()
                    .Where(p => p.FechaPago.Date == fechaConsulta.Date)
                    .ToList();

                var totalEfectivo = pagos.Where(p => p.TipoPago == "Efectivo").Sum(p => p.Monto);
                var totalTransferencia = pagos.Where(p => p.TipoPago == "Transferencia").Sum(p => p.Monto);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fecha = fechaConsulta.ToString("yyyy-MM-dd"),
                        totalPagos = pagos.Count,
                        montoTotal = pagos.Sum(p => p.Monto),
                        efectivo = totalEfectivo,
                        transferencia = totalTransferencia
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener resumen" });
            }
        }

        /// <summary>
        /// Obtener total de ingresos - GET /api/movil/pagos/total-ingresos
        /// </summary>
        [HttpGet("total-ingresos")]
        public IActionResult GetTotalIngresos()
        {
            try
            {
                var total = _pagoService.CalcularTotalIngresos();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalIngresos = total
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener total de ingresos" });
            }
        }
    }

    public class PagoCreateRequest
    {
        public int FacturaId { get; set; }
        public decimal Monto { get; set; }
        public string? Moneda { get; set; }
        public string? TipoPago { get; set; }
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal? Vuelto { get; set; }
        public decimal? TipoCambio { get; set; }
        public string? Observaciones { get; set; }
    }

    public class PagoMultipleRequest
    {
        public List<int> FacturaIds { get; set; } = new();
        public decimal MontoTotal { get; set; }
        public string? Moneda { get; set; }
        public string? TipoPago { get; set; }
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal? Vuelto { get; set; }
        public decimal? TipoCambio { get; set; }
        public string? Observaciones { get; set; }
    }
}

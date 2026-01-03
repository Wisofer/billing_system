using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Services;
using billing_system.Models.Entities;
using billing_system.Utils;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FacturasController : ControllerBase
    {
        private readonly IFacturaService _facturaService;
        private readonly IClienteService _clienteService;
        private readonly IPdfService _pdfService;

        public FacturasController(
            IFacturaService facturaService, 
            IClienteService clienteService,
            IPdfService pdfService)
        {
            _facturaService = facturaService;
            _clienteService = clienteService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Obtener todas las facturas - GET /api/movil/facturas
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20,
            [FromQuery] string? estado = null,
            [FromQuery] string? categoria = null,
            [FromQuery] int? mes = null,
            [FromQuery] int? anio = null)
        {
            try
            {
                var facturas = _facturaService.ObtenerTodas();

                // Filtros
                if (!string.IsNullOrWhiteSpace(estado))
                {
                    facturas = facturas.Where(f => f.Estado == estado).ToList();
                }
                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    facturas = facturas.Where(f => f.Categoria == categoria).ToList();
                }
                if (mes.HasValue && anio.HasValue)
                {
                    facturas = facturas.Where(f => f.FechaCreacion.Month == mes && f.FechaCreacion.Year == anio).ToList();
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
                        // Asegurarse de que el cliente esté cargado
                        Models.Entities.Cliente? cliente = null;
                        if (f.Cliente != null)
                        {
                            cliente = f.Cliente;
                        }
                        else if (f.ClienteId > 0)
                        {
                            cliente = _clienteService.ObtenerPorId(f.ClienteId);
                        }

                        return new
                        {
                            id = f.Id,
                            numero = f.Numero,
                            cliente = cliente != null ? new
                            {
                                id = cliente.Id,
                                codigo = cliente.Codigo,
                                nombre = cliente.Nombre,
                                telefono = cliente.Telefono
                            } : null,
                            servicio = new
                            {
                                id = f.Servicio?.Id,
                                nombre = f.Servicio?.Nombre
                            },
                            monto = f.Monto,
                            estado = f.Estado,
                            categoria = f.Categoria,
                            fechaCreacion = f.FechaCreacion,
                            mesFacturacion = f.MesFacturacion
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
        /// Obtener una factura por ID - GET /api/movil/facturas/{id}
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var factura = _facturaService.ObtenerPorId(id);

                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                // Asegurarse de que el cliente esté cargado
                Models.Entities.Cliente? cliente = null;
                if (factura.Cliente != null)
                {
                    cliente = factura.Cliente;
                }
                else if (factura.ClienteId > 0)
                {
                    cliente = _clienteService.ObtenerPorId(factura.ClienteId);
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = factura.Id,
                        numero = factura.Numero,
                        cliente = cliente != null ? new
                        {
                            id = cliente.Id,
                            codigo = cliente.Codigo,
                            nombre = cliente.Nombre,
                            telefono = cliente.Telefono,
                            email = cliente.Email
                        } : null,
                        servicio = new
                        {
                            id = factura.Servicio?.Id,
                            nombre = factura.Servicio?.Nombre,
                            precio = factura.Servicio?.Precio
                        },
                        monto = factura.Monto,
                        estado = factura.Estado,
                        categoria = factura.Categoria,
                        fechaCreacion = factura.FechaCreacion,
                        mesFacturacion = factura.MesFacturacion,
                        pagos = factura.Pagos?.Select(p => new
                        {
                            id = p.Id,
                            monto = p.Monto,
                            fechaPago = p.FechaPago,
                            tipoPago = p.TipoPago
                        })
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener factura" });
            }
        }

        /// <summary>
        /// Obtener facturas de un cliente - GET /api/movil/facturas/cliente/{clienteId}
        /// </summary>
        [HttpGet("cliente/{clienteId}")]
        public IActionResult GetByCliente(int clienteId, [FromQuery] string? estado = null)
        {
            try
            {
                var facturas = _facturaService.ObtenerPorCliente(clienteId);

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    facturas = facturas.Where(f => f.Estado == estado).ToList();
                }

                return Ok(new
                {
                    success = true,
                    data = facturas.Select(f => new
                    {
                        id = f.Id,
                        numero = f.Numero,
                        monto = f.Monto,
                        estado = f.Estado,
                        categoria = f.Categoria,
                        fechaCreacion = f.FechaCreacion
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener facturas del cliente" });
            }
        }

        /// <summary>
        /// Obtener facturas pendientes - GET /api/movil/facturas/pendientes
        /// </summary>
        [HttpGet("pendientes")]
        public IActionResult GetPendientes([FromQuery] int limite = 50)
        {
            try
            {
                var facturas = _facturaService.ObtenerPendientes();

                return Ok(new
                {
                    success = true,
                    total = facturas.Count,
                    data = facturas.Take(limite).Select(f =>
                    {
                        // Asegurarse de que el cliente esté cargado
                        Models.Entities.Cliente? cliente = null;
                        if (f.Cliente != null)
                        {
                            cliente = f.Cliente;
                        }
                        else if (f.ClienteId > 0)
                        {
                            cliente = _clienteService.ObtenerPorId(f.ClienteId);
                        }

                        return new
                        {
                            id = f.Id,
                            numero = f.Numero,
                            cliente = cliente != null ? new
                            {
                                id = cliente.Id,
                                nombre = cliente.Nombre,
                                telefono = cliente.Telefono
                            } : null,
                            monto = f.Monto,
                            fechaCreacion = f.FechaCreacion,
                            mesFacturacion = f.MesFacturacion
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
        /// Crear una nueva factura - POST /api/movil/facturas
        /// </summary>
        [HttpPost]
        public IActionResult Create([FromBody] FacturaCreateRequest request)
        {
            try
            {
                if (request.ClienteId <= 0)
                {
                    return BadRequest(new { success = false, message = "El cliente es requerido" });
                }

                if (request.ServicioId <= 0)
                {
                    return BadRequest(new { success = false, message = "El servicio es requerido" });
                }

                var factura = new Factura
                {
                    ClienteId = request.ClienteId,
                    ServicioId = request.ServicioId,
                    Monto = request.Monto,
                    Estado = SD.EstadoFacturaPendiente,
                    Categoria = request.Categoria ?? SD.CategoriaInternet
                };

                var facturaCreada = _facturaService.Crear(factura);

                return CreatedAtAction(nameof(GetById), new { id = facturaCreada.Id }, new
                {
                    success = true,
                    message = "Factura creada exitosamente",
                    data = new
                    {
                        id = facturaCreada.Id,
                        numero = facturaCreada.Numero,
                        monto = facturaCreada.Monto
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al crear factura" });
            }
        }

        /// <summary>
        /// Descargar PDF de factura - GET /api/movil/facturas/{id}/pdf
        /// </summary>
        [HttpGet("{id}/pdf")]
        public IActionResult DownloadPdf(int id)
        {
            try
            {
                var factura = _facturaService.ObtenerPorId(id);
                
                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                var pdfBytes = _pdfService.GenerarPdfFactura(factura);
                
                return File(pdfBytes, "application/pdf", $"Factura_{factura.Numero}.pdf");
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al generar PDF" });
            }
        }

        /// <summary>
        /// Eliminar una factura - DELETE /api/movil/facturas/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var factura = _facturaService.ObtenerPorId(id);
                
                if (factura == null)
                {
                    return NotFound(new { success = false, message = "Factura no encontrada" });
                }

                if (factura.Estado == SD.EstadoFacturaPagada)
                {
                    return BadRequest(new { success = false, message = "No se puede eliminar una factura pagada" });
                }

                var eliminada = _facturaService.Eliminar(id);

                if (!eliminada)
                {
                    return BadRequest(new { success = false, message = "No se pudo eliminar la factura" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Factura eliminada exitosamente"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al eliminar factura" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de facturas - GET /api/movil/facturas/estadisticas
        /// </summary>
        [HttpGet("estadisticas")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = _facturaService.ObtenerTotal(),
                        pagadas = _facturaService.ObtenerTotalPagadas(),
                        pendientes = _facturaService.ObtenerTotalPendientes(),
                        montoTotal = _facturaService.ObtenerMontoTotal(),
                        totalPendiente = _facturaService.CalcularTotalPendiente(),
                        totalPagado = _facturaService.CalcularTotalPagado()
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas" });
            }
        }
    }

    public class FacturaCreateRequest
    {
        public int ClienteId { get; set; }
        public int ServicioId { get; set; }
        public decimal Monto { get; set; }
        public string? Categoria { get; set; }
    }
}

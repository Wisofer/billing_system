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
        private readonly IClienteService _clienteService;
        private readonly IConfiguracionService _configuracionService;

        public PagosController(
            IPagoService pagoService, 
            IFacturaService facturaService,
            IClienteService clienteService,
            IConfiguracionService configuracionService)
        {
            _pagoService = pagoService;
            _facturaService = facturaService;
            _clienteService = clienteService;
            _configuracionService = configuracionService;
        }

        /// <summary>
        /// Obtener todos los pagos con filtros - GET /api/movil/pagos
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] int pagina = 1,
            [FromQuery] int tamanoPagina = 20,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? tipoPago = null,
            [FromQuery] string? banco = null)
        {
            try
            {
                var pagos = _pagoService.ObtenerTodos().AsQueryable();

                // Filtrar por fecha
                if (fechaInicio.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago >= fechaInicio.Value);
                }
                if (fechaFin.HasValue)
                {
                    pagos = pagos.Where(p => p.FechaPago <= fechaFin.Value);
                }

                // Filtrar por tipo de pago
                if (!string.IsNullOrWhiteSpace(tipoPago) && tipoPago != "Todos")
                {
                    pagos = pagos.Where(p => p.TipoPago == tipoPago);
                }

                // Filtrar por banco
                if (!string.IsNullOrWhiteSpace(banco) && banco != "Todos")
                {
                    pagos = pagos.Where(p => p.Banco == banco);
                }

                // Ordenar por fecha descendente
                var pagosOrdenados = pagos.OrderByDescending(p => p.FechaPago).ThenByDescending(p => p.Id).ToList();

                // Paginación
                var totalItems = pagosOrdenados.Count;
                var totalPages = (int)Math.Ceiling((double)totalItems / tamanoPagina);
                var items = pagosOrdenados.Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina).ToList();

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
                        tipoCuenta = p.TipoCuenta,
                        fechaPago = p.FechaPago,
                        montoCordobasFisico = p.MontoCordobasFisico,
                        montoDolaresFisico = p.MontoDolaresFisico,
                        montoCordobasElectronico = p.MontoCordobasElectronico,
                        montoDolaresElectronico = p.MontoDolaresElectronico,
                        montoRecibido = p.MontoRecibidoFisico,
                        vuelto = p.VueltoFisico,
                        tipoCambio = p.TipoCambio,
                        observaciones = p.Observaciones,
                        factura = p.Factura != null ? new
                        {
                            id = p.Factura.Id,
                            numero = p.Factura.Numero,
                            monto = p.Factura.Monto,
                            cliente = new
                            {
                                id = p.Factura.Cliente?.Id,
                                codigo = p.Factura.Cliente?.Codigo,
                                nombre = p.Factura.Cliente?.Nombre,
                                telefono = p.Factura.Cliente?.Telefono
                            }
                        } : null,
                        facturas = p.PagoFacturas?.Select(pf => new
                        {
                            id = pf.Factura?.Id,
                            numero = pf.Factura?.Numero,
                            monto = pf.MontoAplicado
                        }).ToList()
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
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener pagos: {ex.Message}" });
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
                        montoCordobasFisico = pago.MontoCordobasFisico,
                        montoDolaresFisico = pago.MontoDolaresFisico,
                        montoCordobasElectronico = pago.MontoCordobasElectronico,
                        montoDolaresElectronico = pago.MontoDolaresElectronico,
                        montoRecibido = pago.MontoRecibidoFisico,
                        vuelto = pago.VueltoFisico,
                        tipoCambio = pago.TipoCambio,
                        observaciones = pago.Observaciones,
                        factura = pago.Factura != null ? new
                        {
                            id = pago.Factura.Id,
                            numero = pago.Factura.Numero,
                            monto = pago.Factura.Monto,
                            estado = pago.Factura.Estado,
                            mesFacturacion = pago.Factura.MesFacturacion,
                            cliente = new
                            {
                                id = pago.Factura.Cliente?.Id,
                                codigo = pago.Factura.Cliente?.Codigo,
                                nombre = pago.Factura.Cliente?.Nombre,
                                telefono = pago.Factura.Cliente?.Telefono,
                                email = pago.Factura.Cliente?.Email
                            }
                        } : null,
                        facturas = pago.PagoFacturas?.Select(pf => new
                        {
                            id = pf.Factura?.Id,
                            numero = pf.Factura?.Numero,
                            monto = pf.Factura?.Monto,
                            montoAplicado = pf.MontoAplicado
                        }).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener pago: {ex.Message}" });
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
                var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        tipoCambio = tipoCambio,
                        monedaBase = "USD",
                        monedaDestino = "NIO",
                        fechaActualizacion = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener tipo de cambio: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener facturas pendientes de un cliente para pago - GET /api/movil/pagos/facturas-cliente/{clienteId}
        /// </summary>
        [HttpGet("facturas-cliente/{clienteId}")]
        public IActionResult GetFacturasCliente(int clienteId)
        {
            try
            {
                var cliente = _clienteService.ObtenerPorId(clienteId);
                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                // Obtener todas las facturas del cliente
                var facturas = _facturaService.ObtenerPorCliente(clienteId)
                    .OrderByDescending(f => f.MesFacturacion)
                    .ThenByDescending(f => f.FechaCreacion)
                    .ToList();

                // Calcular saldo pendiente de cada factura
                var facturasConSaldo = facturas.Select(f =>
                {
                    var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
                    var saldoPendiente = f.Monto - totalPagado;
                    var saldoPendienteAjustado = saldoPendiente < 0 ? 0 : saldoPendiente;

                    return new
                    {
                        id = f.Id,
                        numero = f.Numero,
                        monto = f.Monto,
                        mesFacturacion = f.MesFacturacion,
                        mesNombre = f.MesFacturacion.ToString("MMMM yyyy"),
                        estado = f.Estado,
                        categoria = f.Categoria,
                        totalPagado = totalPagado,
                        saldoPendiente = saldoPendienteAjustado,
                        puedePagar = saldoPendienteAjustado > 0,
                        servicio = f.Servicio != null ? new
                        {
                            id = f.Servicio.Id,
                            nombre = f.Servicio.Nombre
                        } : null
                    };
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        cliente = new
                        {
                            id = cliente.Id,
                            codigo = cliente.Codigo,
                            nombre = cliente.Nombre,
                            telefono = cliente.Telefono
                        },
                        facturas = facturasConSaldo,
                        resumen = new
                        {
                            totalFacturas = facturas.Count,
                            facturasPendientes = facturasConSaldo.Count(f => f.estado == "Pendiente"),
                            facturasPagadas = facturasConSaldo.Count(f => f.estado == "Pagada"),
                            saldoTotalPendiente = facturasConSaldo.Sum(f => f.saldoPendiente)
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener facturas del cliente: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener facturas pendientes de pago - GET /api/movil/pagos/facturas-pendientes
        /// </summary>
        [HttpGet("facturas-pendientes")]
        public IActionResult GetFacturasPendientes([FromQuery] int limite = 50, [FromQuery] string? busqueda = null)
        {
            try
            {
                var facturas = _facturaService.ObtenerTodas()
                    .Where(f => f.Estado == SD.EstadoFacturaPendiente)
                    .AsQueryable();

                // Filtrar por búsqueda (cliente o número de factura)
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var busquedaLower = busqueda.ToLower();
                    facturas = facturas.Where(f =>
                        (f.Cliente != null && f.Cliente.Nombre.ToLower().Contains(busquedaLower)) ||
                        (f.Cliente != null && f.Cliente.Codigo.ToLower().Contains(busquedaLower)) ||
                        f.Numero.ToLower().Contains(busquedaLower));
                }

                var facturasLista = facturas
                    .OrderByDescending(f => f.MesFacturacion)
                    .Take(limite)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = facturasLista.Select(f =>
                    {
                        var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
                        var saldoPendiente = f.Monto - totalPagado;

                        return new
                        {
                            id = f.Id,
                            numero = f.Numero,
                            monto = f.Monto,
                            saldoPendiente = saldoPendiente > 0 ? saldoPendiente : 0,
                            mesFacturacion = f.MesFacturacion,
                            categoria = f.Categoria,
                            cliente = new
                            {
                                id = f.Cliente?.Id,
                                codigo = f.Cliente?.Codigo,
                                nombre = f.Cliente?.Nombre,
                                telefono = f.Cliente?.Telefono
                            }
                        };
                    }),
                    totalFacturas = facturasLista.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener facturas pendientes: {ex.Message}" });
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

                // Validar saldo pendiente
                var totalPagado = _pagoService.ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
                var saldoPendiente = factura.Monto - totalPagado;
                
                if (saldoPendiente <= 0)
                {
                    return BadRequest(new { success = false, message = "La factura no tiene saldo pendiente" });
                }

                var monto = request.Monto > 0 ? request.Monto : saldoPendiente;

                var pago = new Pago
                {
                    FacturaId = request.FacturaId,
                    Monto = monto,
                    Moneda = request.Moneda ?? "NIO",
                    TipoPago = request.TipoPago ?? "Fisico",
                    Banco = request.Banco,
                    TipoCuenta = request.TipoCuenta,
                    MontoCordobasFisico = request.MontoCordobasFisico,
                    MontoDolaresFisico = request.MontoDolaresFisico,
                    MontoCordobasElectronico = request.MontoCordobasElectronico,
                    MontoDolaresElectronico = request.MontoDolaresElectronico,
                    MontoRecibidoFisico = request.MontoRecibido,
                    VueltoFisico = request.Vuelto,
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
                        moneda = pagoCreado.Moneda,
                        tipoPago = pagoCreado.TipoPago,
                        fechaPago = pagoCreado.FechaPago,
                        facturaNumero = factura.Numero,
                        clienteNombre = factura.Cliente?.Nombre
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al registrar pago: {ex.Message}" });
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

                // Validar que todas las facturas existan y estén pendientes
                var facturas = request.FacturaIds
                    .Select(id => _facturaService.ObtenerPorId(id))
                    .Where(f => f != null)
                    .ToList();

                if (facturas.Count != request.FacturaIds.Count)
                {
                    return BadRequest(new { success = false, message = "Una o más facturas no fueron encontradas" });
                }

                // Verificar que todas pertenezcan al mismo cliente
                var primerClienteId = facturas.First()!.ClienteId;
                if (facturas.Any(f => f!.ClienteId != primerClienteId))
                {
                    return BadRequest(new { success = false, message = "Todas las facturas deben pertenecer al mismo cliente" });
                }

                // Calcular monto total si no se proporciona
                var montoTotal = request.MontoTotal > 0 ? request.MontoTotal : facturas.Sum(f => f!.Monto);

                var pago = new Pago
                {
                    Monto = montoTotal,
                    Moneda = request.Moneda ?? "NIO",
                    TipoPago = request.TipoPago ?? "Fisico",
                    Banco = request.Banco,
                    TipoCuenta = request.TipoCuenta,
                    MontoCordobasFisico = request.MontoCordobasFisico,
                    MontoDolaresFisico = request.MontoDolaresFisico,
                    MontoCordobasElectronico = request.MontoCordobasElectronico,
                    MontoDolaresElectronico = request.MontoDolaresElectronico,
                    MontoRecibidoFisico = request.MontoRecibido,
                    VueltoFisico = request.Vuelto,
                    TipoCambio = request.TipoCambio,
                    Observaciones = request.Observaciones,
                    FechaPago = DateTime.Now
                };

                var pagoCreado = _pagoService.Crear(pago, request.FacturaIds);

                return Ok(new
                {
                    success = true,
                    message = $"Pago registrado para {request.FacturaIds.Count} factura(s)",
                    data = new
                    {
                        id = pagoCreado.Id,
                        monto = pagoCreado.Monto,
                        moneda = pagoCreado.Moneda,
                        tipoPago = pagoCreado.TipoPago,
                        cantidadFacturas = request.FacturaIds.Count,
                        fechaPago = pagoCreado.FechaPago
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al registrar pago múltiple: {ex.Message}" });
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

                var eliminado = _pagoService.Eliminar(id);

                if (eliminado)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Pago eliminado exitosamente"
                    });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "No se pudo eliminar el pago" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al eliminar pago: {ex.Message}" });
            }
        }

        /// <summary>
        /// Eliminar múltiples pagos - DELETE /api/movil/pagos/multiples
        /// </summary>
        [HttpDelete("multiples")]
        public IActionResult DeleteMultiple([FromBody] EliminarMultiplesRequest request)
        {
            try
            {
                if (request.PagoIds == null || !request.PagoIds.Any())
                {
                    return BadRequest(new { success = false, message = "Debe seleccionar al menos un pago" });
                }

                var resultado = _pagoService.EliminarMultiples(request.PagoIds);

                return Ok(new
                {
                    success = true,
                    message = $"{resultado.eliminados} pago(s) eliminado(s)",
                    data = new
                    {
                        eliminados = resultado.eliminados,
                        noEncontrados = resultado.noEncontrados
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al eliminar pagos: {ex.Message}" });
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

                var totalEfectivo = pagos.Where(p => p.TipoPago == SD.TipoPagoFisico).Sum(p => p.Monto);
                var totalElectronico = pagos.Where(p => p.TipoPago == SD.TipoPagoElectronico).Sum(p => p.Monto);
                var totalMixto = pagos.Where(p => p.TipoPago == SD.TipoPagoMixto).Sum(p => p.Monto);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        fecha = fechaConsulta.ToString("yyyy-MM-dd"),
                        fechaFormateada = fechaConsulta.ToString("dddd, dd MMMM yyyy"),
                        totalPagos = pagos.Count,
                        montoTotal = pagos.Sum(p => p.Monto),
                        porTipoPago = new
                        {
                            fisico = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoFisico),
                                monto = totalEfectivo
                            },
                            electronico = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoElectronico),
                                monto = totalElectronico
                            },
                            mixto = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoMixto),
                                monto = totalMixto
                            }
                        },
                        porBanco = pagos
                            .Where(p => !string.IsNullOrEmpty(p.Banco))
                            .GroupBy(p => p.Banco)
                            .Select(g => new
                            {
                                banco = g.Key,
                                cantidad = g.Count(),
                                monto = g.Sum(p => p.Monto)
                            })
                            .ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener resumen: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener resumen de pagos por período - GET /api/movil/pagos/resumen-periodo
        /// </summary>
        [HttpGet("resumen-periodo")]
        public IActionResult GetResumenPeriodo(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] int? mes = null,
            [FromQuery] int? anio = null)
        {
            try
            {
                // Si se proporciona mes y año, usar esos valores
                if (mes.HasValue && anio.HasValue)
                {
                    fechaInicio = new DateTime(anio.Value, mes.Value, 1);
                    fechaFin = fechaInicio.Value.AddMonths(1).AddDays(-1);
                }
                // Si no se proporciona nada, usar el mes actual
                else if (!fechaInicio.HasValue && !fechaFin.HasValue)
                {
                    fechaInicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    fechaFin = DateTime.Now;
                }

                var pagos = _pagoService.ObtenerTodos()
                    .Where(p => p.FechaPago >= fechaInicio && p.FechaPago <= fechaFin)
                    .ToList();

                // Agrupar por día
                var pagosPorDia = pagos
                    .GroupBy(p => p.FechaPago.Date)
                    .Select(g => new
                    {
                        fecha = g.Key.ToString("yyyy-MM-dd"),
                        cantidad = g.Count(),
                        monto = g.Sum(p => p.Monto)
                    })
                    .OrderBy(x => x.fecha)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        periodo = new
                        {
                            inicio = fechaInicio?.ToString("yyyy-MM-dd"),
                            fin = fechaFin?.ToString("yyyy-MM-dd")
                        },
                        resumen = new
                        {
                            totalPagos = pagos.Count,
                            montoTotal = pagos.Sum(p => p.Monto),
                            promedioProPago = pagos.Any() ? pagos.Average(p => p.Monto) : 0
                        },
                        porTipoPago = new
                        {
                            fisico = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoFisico),
                                monto = pagos.Where(p => p.TipoPago == SD.TipoPagoFisico).Sum(p => p.Monto)
                            },
                            electronico = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoElectronico),
                                monto = pagos.Where(p => p.TipoPago == SD.TipoPagoElectronico).Sum(p => p.Monto)
                            },
                            mixto = new
                            {
                                cantidad = pagos.Count(p => p.TipoPago == SD.TipoPagoMixto),
                                monto = pagos.Where(p => p.TipoPago == SD.TipoPagoMixto).Sum(p => p.Monto)
                            }
                        },
                        porBanco = pagos
                            .Where(p => !string.IsNullOrEmpty(p.Banco))
                            .GroupBy(p => p.Banco)
                            .Select(g => new
                            {
                                banco = g.Key,
                                cantidad = g.Count(),
                                monto = g.Sum(p => p.Monto)
                            })
                            .OrderByDescending(x => x.monto)
                            .ToList(),
                        pagosPorDia = pagosPorDia
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener resumen: {ex.Message}" });
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
                var todosPagos = _pagoService.ObtenerTodos();

                // Calcular ingresos del mes actual
                var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var ingresosMesActual = todosPagos
                    .Where(p => p.FechaPago >= inicioMes)
                    .Sum(p => p.Monto);

                // Calcular ingresos de hoy
                var ingresosHoy = todosPagos
                    .Where(p => p.FechaPago.Date == DateTime.Today)
                    .Sum(p => p.Monto);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        totalIngresos = total,
                        ingresosMesActual = ingresosMesActual,
                        ingresosHoy = ingresosHoy,
                        totalPagos = todosPagos.Count
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener total de ingresos: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de pagos - GET /api/movil/pagos/estadisticas
        /// </summary>
        [HttpGet("estadisticas")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                var todosPagos = _pagoService.ObtenerTodos();
                var totalIngresos = _pagoService.CalcularTotalIngresos();

                // Estadísticas por tipo de pago
                var pagosFisicos = todosPagos.Where(p => p.TipoPago == SD.TipoPagoFisico).ToList();
                var pagosElectronicos = todosPagos.Where(p => p.TipoPago == SD.TipoPagoElectronico).ToList();
                var pagosMixtos = todosPagos.Where(p => p.TipoPago == SD.TipoPagoMixto).ToList();

                // Estadísticas del mes actual
                var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var pagosMesActual = todosPagos.Where(p => p.FechaPago >= inicioMes).ToList();

                // Bancos disponibles
                var bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        general = new
                        {
                            totalPagos = todosPagos.Count,
                            totalIngresos = totalIngresos,
                            promedioProPago = todosPagos.Any() ? todosPagos.Average(p => p.Monto) : 0
                        },
                        porTipoPago = new
                        {
                            fisico = new
                            {
                                cantidad = pagosFisicos.Count,
                                monto = pagosFisicos.Sum(p => p.Monto),
                                porcentaje = todosPagos.Any() ? (decimal)pagosFisicos.Count / todosPagos.Count * 100 : 0
                            },
                            electronico = new
                            {
                                cantidad = pagosElectronicos.Count,
                                monto = pagosElectronicos.Sum(p => p.Monto),
                                porcentaje = todosPagos.Any() ? (decimal)pagosElectronicos.Count / todosPagos.Count * 100 : 0
                            },
                            mixto = new
                            {
                                cantidad = pagosMixtos.Count,
                                monto = pagosMixtos.Sum(p => p.Monto),
                                porcentaje = todosPagos.Any() ? (decimal)pagosMixtos.Count / todosPagos.Count * 100 : 0
                            }
                        },
                        mesActual = new
                        {
                            mes = DateTime.Now.ToString("MMMM yyyy"),
                            totalPagos = pagosMesActual.Count,
                            totalIngresos = pagosMesActual.Sum(p => p.Monto)
                        },
                        porBanco = todosPagos
                            .Where(p => !string.IsNullOrEmpty(p.Banco))
                            .GroupBy(p => p.Banco)
                            .Select(g => new
                            {
                                banco = g.Key,
                                cantidad = g.Count(),
                                monto = g.Sum(p => p.Monto)
                            })
                            .OrderByDescending(x => x.cantidad)
                            .ToList(),
                        configuracion = new
                        {
                            bancos = bancos,
                            tiposPago = new[] { "Fisico", "Electronico", "Mixto" },
                            monedas = new[] { "NIO", "USD", "Ambos" },
                            tiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error al obtener estadísticas: {ex.Message}" });
            }
        }

        /// <summary>
        /// Buscar pagos por cliente - GET /api/movil/pagos/buscar
        /// </summary>
        [HttpGet("buscar")]
        public IActionResult Buscar([FromQuery] string q, [FromQuery] int limite = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { success = false, message = "El término de búsqueda es requerido" });
                }

                var busquedaLower = q.ToLower();
                var pagos = _pagoService.ObtenerTodos()
                    .Where(p => 
                        (p.Factura?.Cliente?.Nombre?.ToLower().Contains(busquedaLower) ?? false) ||
                        (p.Factura?.Cliente?.Codigo?.ToLower().Contains(busquedaLower) ?? false) ||
                        (p.Factura?.Numero?.ToLower().Contains(busquedaLower) ?? false))
                    .OrderByDescending(p => p.FechaPago)
                    .Take(limite)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = pagos.Select(p => new
                    {
                        id = p.Id,
                        monto = p.Monto,
                        moneda = p.Moneda,
                        tipoPago = p.TipoPago,
                        fechaPago = p.FechaPago,
                        factura = new
                        {
                            id = p.Factura?.Id,
                            numero = p.Factura?.Numero
                        },
                        cliente = new
                        {
                            id = p.Factura?.Cliente?.Id,
                            codigo = p.Factura?.Cliente?.Codigo,
                            nombre = p.Factura?.Cliente?.Nombre
                        }
                    }),
                    totalResultados = pagos.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error en la búsqueda: {ex.Message}" });
            }
        }
    }

    // ==================== MODELOS DE REQUEST ====================

    public class PagoCreateRequest
    {
        public int FacturaId { get; set; }
        public decimal Monto { get; set; }
        public string? Moneda { get; set; }
        public string? TipoPago { get; set; }
        public string? Banco { get; set; }
        public string? TipoCuenta { get; set; }
        public decimal? MontoCordobasFisico { get; set; }
        public decimal? MontoDolaresFisico { get; set; }
        public decimal? MontoCordobasElectronico { get; set; }
        public decimal? MontoDolaresElectronico { get; set; }
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
        public decimal? MontoCordobasFisico { get; set; }
        public decimal? MontoDolaresFisico { get; set; }
        public decimal? MontoCordobasElectronico { get; set; }
        public decimal? MontoDolaresElectronico { get; set; }
        public decimal? MontoRecibido { get; set; }
        public decimal? Vuelto { get; set; }
        public decimal? TipoCambio { get; set; }
        public string? Observaciones { get; set; }
    }

    public class EliminarMultiplesRequest
    {
        public List<int> PagoIds { get; set; } = new();
    }
}

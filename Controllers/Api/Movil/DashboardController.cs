using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly IFacturaService _facturaService;
        private readonly IPagoService _pagoService;
        private readonly IEgresoService _egresoService;

        public DashboardController(
            IClienteService clienteService,
            IFacturaService facturaService,
            IPagoService pagoService,
            IEgresoService egresoService)
        {
            _clienteService = clienteService;
            _facturaService = facturaService;
            _pagoService = pagoService;
            _egresoService = egresoService;
        }

        /// <summary>
        /// Obtener estadísticas generales del dashboard - GET /api/movil/dashboard
        /// </summary>
        [HttpGet]
        public IActionResult GetDashboard()
        {
            try
            {
                // Clientes
                var totalClientes = _clienteService.ObtenerTotal();
                var clientesActivos = _clienteService.ObtenerTotalActivos();

                // Facturas
                var facturasPendientes = _facturaService.ObtenerPendientes();
                var totalPendiente = _facturaService.CalcularTotalPendiente();

                // Pagos
                var totalIngresos = _pagoService.CalcularTotalIngresos();

                // Egresos del mes
                var egresosMes = _egresoService.CalcularTotalEgresosMesActual();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        clientes = new
                        {
                            total = totalClientes,
                            activos = clientesActivos,
                            nuevosEsteMes = _clienteService.ObtenerNuevosEsteMes()
                        },
                        facturas = new
                        {
                            total = _facturaService.ObtenerTotal(),
                            pendientes = facturasPendientes.Count,
                            pagadas = _facturaService.ObtenerTotalPagadas(),
                            montoPendiente = totalPendiente,
                            montoPagado = _facturaService.CalcularTotalPagado()
                        },
                        ingresos = new
                        {
                            total = totalIngresos
                        },
                        egresos = new
                        {
                            mesActual = egresosMes,
                            total = _egresoService.CalcularTotalEgresos()
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

        /// <summary>
        /// Obtener estadísticas de clientes - GET /api/movil/dashboard/clientes
        /// </summary>
        [HttpGet("clientes")]
        public IActionResult GetClientesStats()
        {
            try
            {
                var clientes = _clienteService.ObtenerTodos();

                var porEstado = clientes
                    .GroupBy(c => c.Activo ? "Activo" : "Inactivo")
                    .Select(g => new { estado = g.Key, cantidad = g.Count() })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = clientes.Count,
                        porEstado
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas de clientes" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de facturas - GET /api/movil/dashboard/facturas
        /// </summary>
        [HttpGet("facturas")]
        public IActionResult GetFacturasStats([FromQuery] int? mes = null, [FromQuery] int? anio = null)
        {
            try
            {
                var mesConsulta = mes ?? DateTime.Now.Month;
                var anioConsulta = anio ?? DateTime.Now.Year;

                var fechaConsulta = new DateTime(anioConsulta, mesConsulta, 1);
                var facturas = _facturaService.ObtenerPorMes(fechaConsulta);

                var pagadas = facturas.Where(f => f.Estado == SD.EstadoFacturaPagada).ToList();
                var pendientes = facturas.Where(f => f.Estado == SD.EstadoFacturaPendiente).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        mes = mesConsulta,
                        anio = anioConsulta,
                        total = facturas.Count,
                        pagadas = new
                        {
                            cantidad = pagadas.Count,
                            monto = pagadas.Sum(f => f.Monto)
                        },
                        pendientes = new
                        {
                            cantidad = pendientes.Count,
                            monto = pendientes.Sum(f => f.Monto)
                        },
                        porCategoria = facturas
                            .GroupBy(f => f.Categoria)
                            .Select(g => new
                            {
                                categoria = g.Key,
                                cantidad = g.Count(),
                                monto = g.Sum(f => f.Monto)
                            })
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas de facturas" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de pagos - GET /api/movil/dashboard/pagos
        /// </summary>
        [HttpGet("pagos")]
        public IActionResult GetPagosStats([FromQuery] int? mes = null, [FromQuery] int? anio = null)
        {
            try
            {
                var mesConsulta = mes ?? DateTime.Now.Month;
                var anioConsulta = anio ?? DateTime.Now.Year;

                var pagos = _pagoService.ObtenerTodos()
                    .Where(p => p.FechaPago.Month == mesConsulta && p.FechaPago.Year == anioConsulta)
                    .ToList();

                var porTipoPago = pagos
                    .GroupBy(p => p.TipoPago)
                    .Select(g => new
                    {
                        tipo = g.Key,
                        cantidad = g.Count(),
                        monto = g.Sum(p => p.Monto)
                    })
                    .ToList();

                var porDia = pagos
                    .GroupBy(p => p.FechaPago.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        fecha = g.Key.ToString("yyyy-MM-dd"),
                        cantidad = g.Count(),
                        monto = g.Sum(p => p.Monto)
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        mes = mesConsulta,
                        anio = anioConsulta,
                        total = pagos.Count,
                        montoTotal = pagos.Sum(p => p.Monto),
                        porTipoPago,
                        porDia
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas de pagos" });
            }
        }

        /// <summary>
        /// Obtener resumen rápido - GET /api/movil/dashboard/resumen
        /// </summary>
        [HttpGet("resumen")]
        public IActionResult GetResumen()
        {
            try
            {
                var hoy = DateTime.Today;
                var pagosHoy = _pagoService.ObtenerTodos()
                    .Where(p => p.FechaPago.Date == hoy)
                    .ToList();
                    
                var facturasPendientes = _facturaService.ObtenerPendientes();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        pagosHoy = new
                        {
                            cantidad = pagosHoy.Count,
                            monto = pagosHoy.Sum(p => p.Monto)
                        },
                        facturasPendientes = new
                        {
                            cantidad = facturasPendientes.Count,
                            monto = facturasPendientes.Sum(f => f.Monto)
                        },
                        fecha = hoy.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener resumen" });
            }
        }

        /// <summary>
        /// Obtener egresos por categoría - GET /api/movil/dashboard/egresos
        /// </summary>
        [HttpGet("egresos")]
        public IActionResult GetEgresosStats()
        {
            try
            {
                var egresosPorCategoria = _egresoService.ObtenerEgresosPorCategoria();
                var ultimosEgresos = _egresoService.ObtenerUltimosEgresos(10);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = _egresoService.CalcularTotalEgresos(),
                        mesActual = _egresoService.CalcularTotalEgresosMesActual(),
                        porCategoria = egresosPorCategoria,
                        ultimos = ultimosEgresos.Select(e => new
                        {
                            id = e.Id,
                            codigo = e.Codigo,
                            descripcion = e.Descripcion,
                            monto = e.Monto,
                            fecha = e.Fecha,
                            categoria = e.Categoria
                        })
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas de egresos" });
            }
        }
    }
}

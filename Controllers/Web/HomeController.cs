using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Data;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Controllers.Web;

[Authorize]
[Route("[action]")]
public class HomeController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IPagoService _pagoService;
    private readonly IClienteService _clienteService;
    private readonly IEgresoService _egresoService;
    private readonly ApplicationDbContext _context;

    public HomeController(IFacturaService facturaService, IPagoService pagoService, IClienteService clienteService, IEgresoService egresoService, ApplicationDbContext context)
    {
        _facturaService = facturaService;
        _pagoService = pagoService;
        _clienteService = clienteService;
        _egresoService = egresoService;
        _context = context;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        // Si el usuario no es Administrador, redirigir según su rol
        var rol = SecurityHelper.GetUserRole(User);
        if (rol != SD.RolAdministrador)
        {
            return Redirect(SecurityHelper.GetRedirectUrlByRole(User));
        }
        // Calcular fechas del mes actual
        var fechaMesActual = DateTime.Now;
        var primerDiaMes = new DateTime(fechaMesActual.Year, fechaMesActual.Month, 1);
        var ultimoDiaMes = primerDiaMes.AddMonths(1).AddDays(-1).Date.AddDays(1).AddSeconds(-1);
        var mesActualTexto = fechaMesActual.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-NI"));

        // Estadísticas del mes actual
        var pagosMesActual = _context.Pagos
            .Where(p => p.FechaPago >= primerDiaMes && p.FechaPago <= ultimoDiaMes)
            .ToList();
        
        var ingresosMesActual = pagosMesActual.Sum(p => p.Monto);
        
        var egresosMesActual = _context.Egresos
            .Where(e => e.Fecha >= primerDiaMes && e.Fecha <= ultimoDiaMes)
            .Sum(e => (decimal?)e.Monto) ?? 0;
        
        var facturasMesActual = _context.Facturas
            .Count(f => f.FechaCreacion >= primerDiaMes && f.FechaCreacion <= ultimoDiaMes);
        
        var balanceMesActual = ingresosMesActual - egresosMesActual;

        // Ingresos del mes actual - Internet (pagos del mes actual relacionados con facturas de Internet)
        var pagosMesActualConFacturas = _context.Pagos
            .Include(p => p.Factura)
            .Include(p => p.PagoFacturas)
            .ThenInclude(pf => pf.Factura)
            .Where(p => p.FechaPago >= primerDiaMes && p.FechaPago <= ultimoDiaMes)
            .ToList();
        
        decimal ingresosMesActualInternet = 0;
        var facturasConPagosInternet = new HashSet<int>(); // Para evitar duplicados
        var facturasPagadasEsteMesInternet = new HashSet<int>();
        
        foreach (var pago in pagosMesActualConFacturas)
        {
            // Procesar factura directa
            if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaInternet)
            {
                ingresosMesActualInternet += pago.Monto;
                facturasConPagosInternet.Add(pago.Factura.Id);
                // Si la factura está pagada, agregarla a la lista
                if (pago.Factura.Estado == SD.EstadoFacturaPagada)
                {
                    facturasPagadasEsteMesInternet.Add(pago.Factura.Id);
                }
            }
            
            // Procesar facturas a través de PagoFacturas
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaInternet)
                {
                    ingresosMesActualInternet += pagoFactura.MontoAplicado;
                    facturasConPagosInternet.Add(pagoFactura.Factura.Id);
                    // Si la factura está pagada, agregarla a la lista
                    if (pagoFactura.Factura.Estado == SD.EstadoFacturaPagada)
                    {
                        facturasPagadasEsteMesInternet.Add(pagoFactura.Factura.Id);
                    }
                }
            }
        }

        // Estadísticas del mes actual por categoría - Internet
        // Facturas que recibieron pagos este mes
        var facturasMesActualInternet = facturasConPagosInternet.Count;
        var facturasPagadasMesActualInternet = facturasPagadasEsteMesInternet.Count;
        var facturasPendientesMesActualInternet = facturasMesActualInternet - facturasPagadasMesActualInternet;

        // Ingresos del mes actual - Streaming
        decimal ingresosMesActualStreaming = 0;
        var facturasConPagosStreaming = new HashSet<int>(); // Para evitar duplicados
        var facturasPagadasEsteMesStreaming = new HashSet<int>();
        
        foreach (var pago in pagosMesActualConFacturas)
        {
            // Procesar factura directa
            if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaStreaming)
            {
                ingresosMesActualStreaming += pago.Monto;
                facturasConPagosStreaming.Add(pago.Factura.Id);
                // Si la factura está pagada, agregarla a la lista
                if (pago.Factura.Estado == SD.EstadoFacturaPagada)
                {
                    facturasPagadasEsteMesStreaming.Add(pago.Factura.Id);
                }
            }
            
            // Procesar facturas a través de PagoFacturas
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaStreaming)
                {
                    ingresosMesActualStreaming += pagoFactura.MontoAplicado;
                    facturasConPagosStreaming.Add(pagoFactura.Factura.Id);
                    // Si la factura está pagada, agregarla a la lista
                    if (pagoFactura.Factura.Estado == SD.EstadoFacturaPagada)
                    {
                        facturasPagadasEsteMesStreaming.Add(pagoFactura.Factura.Id);
                    }
                }
            }
        }

        // Estadísticas del mes actual por categoría - Streaming
        // Facturas que recibieron pagos este mes
        var facturasMesActualStreaming = facturasConPagosStreaming.Count;
        var facturasPagadasMesActualStreaming = facturasPagadasEsteMesStreaming.Count;
        var facturasPendientesMesActualStreaming = facturasMesActualStreaming - facturasPagadasMesActualStreaming;

        // Estadísticas generales (consultas optimizadas)
        var pagosPendientes = _facturaService.CalcularTotalPendiente();
        var pagosRealizados = _pagoService.CalcularTotalIngresos();
        var totalClientes = _clienteService.ObtenerTotal();
        var totalFacturas = _facturaService.ObtenerTotal();
        var totalPagos = _context.Pagos.Count(); // Optimizado: Count directo en BD
        var totalClientesActivos = _clienteService.ObtenerTotalActivos();

        // Estadísticas por categoría - Internet (optimizado: agregaciones directas en BD)
        var facturasInternetCount = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaInternet)
            .Count();
        
        var facturasPagadasInternet = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaInternet && f.Estado == SD.EstadoFacturaPagada)
            .Count();
        
        var facturasPendientesInternet = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaInternet && f.Estado == SD.EstadoFacturaPendiente)
            .Count();
        
        var pendientesInternet = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaInternet && f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => (decimal?)f.Monto) ?? 0;
        
        // Ingresos Internet: incluir pagos directos y pagos con múltiples facturas
        // Procesar en memoria para calcular correctamente el monto por categoría
        var pagosInternet = _context.Pagos
            .Include(p => p.Factura)
            .Include(p => p.PagoFacturas)
            .ThenInclude(pf => pf.Factura)
            .ToList();
        
        decimal ingresosInternet = 0;
        foreach (var pago in pagosInternet)
        {
            // Si tiene Factura directa de Internet
            if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaInternet)
            {
                ingresosInternet += pago.Monto;
            }
            
            // Si tiene PagoFacturas de Internet, sumar el MontoAplicado de cada una
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaInternet)
                {
                    ingresosInternet += pagoFactura.MontoAplicado;
                }
            }
        }

        // Estadísticas por categoría - Streaming (optimizado: agregaciones directas en BD)
        var facturasStreamingCount = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming)
            .Count();
        
        var facturasPagadasStreaming = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming && f.Estado == SD.EstadoFacturaPagada)
            .Count();
        
        var facturasPendientesStreaming = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming && f.Estado == SD.EstadoFacturaPendiente)
            .Count();
        
        var pendientesStreaming = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming && f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => (decimal?)f.Monto) ?? 0;
        
        // Ingresos Streaming: incluir pagos directos y pagos con múltiples facturas
        // Procesar en memoria para calcular correctamente el monto por categoría
        var pagosStreaming = _context.Pagos
            .Include(p => p.Factura)
            .Include(p => p.PagoFacturas)
            .ThenInclude(pf => pf.Factura)
            .ToList();
        
        decimal ingresosStreaming = 0;
        foreach (var pago in pagosStreaming)
        {
            // Si tiene Factura directa de Streaming
            if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaStreaming)
            {
                ingresosStreaming += pago.Monto;
            }
            
            // Si tiene PagoFacturas de Streaming, sumar el MontoAplicado de cada una
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaStreaming)
                {
                    ingresosStreaming += pagoFactura.MontoAplicado;
                }
            }
        }

        // Estadísticas de clientes por tipo de servicio (optimizado: consultas directas en BD)
        var clientesConInternet = _context.Clientes
            .Where(c => c.Activo && 
                       c.ClienteServicios.Any(cs => cs.Activo && 
                                                  cs.Servicio != null && 
                                                  cs.Servicio.Categoria == SD.CategoriaInternet))
            .Count();
        
        var clientesConStreaming = _context.Clientes
            .Where(c => c.Activo && 
                       c.ClienteServicios.Any(cs => cs.Activo && 
                                                  cs.Servicio != null && 
                                                  cs.Servicio.Categoria == SD.CategoriaStreaming))
            .Count();
        
        var clientesConAmbos = _context.Clientes
            .Where(c => c.Activo && 
                       c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaInternet) &&
                       c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaStreaming))
            .Count();

        // Estadísticas mensuales (optimizado: una sola consulta agrupada en lugar de 24 consultas)
        var fechaActual = DateTime.Now;
        var fechaInicio = fechaActual.AddMonths(-5).Date;
        fechaInicio = new DateTime(fechaInicio.Year, fechaInicio.Month, 1);
        
        // Obtener todas las facturas de los últimos 6 meses en una sola consulta
        var facturasMensuales = _context.Facturas
            .Where(f => f.MesFacturacion >= fechaInicio)
            .GroupBy(f => new { 
                Ano = f.MesFacturacion.Year, 
                Mes = f.MesFacturacion.Month, 
                Categoria = f.Categoria 
            })
            .Select(g => new {
                g.Key.Ano,
                g.Key.Mes,
                g.Key.Categoria,
                Count = g.Count()
            })
            .ToList();
        
        // Obtener todos los pagos de los últimos 6 meses
        // Cargar primero en memoria y luego procesar (porque EF Core no puede traducir SelectMany complejo)
        var pagosConFacturas = _context.Pagos
            .Include(p => p.Factura)
            .Include(p => p.PagoFacturas)
            .ThenInclude(pf => pf.Factura)
            .Where(p => (p.Factura != null && p.Factura.MesFacturacion >= fechaInicio) ||
                       (p.PagoFacturas.Any(pf => pf.Factura != null && pf.Factura.MesFacturacion >= fechaInicio)))
            .ToList(); // Cargar en memoria para procesar

        // Procesar en memoria para construir las estadísticas mensuales
        var pagosMensuales = new List<dynamic>();
        
        foreach (var pago in pagosConFacturas)
        {
            // Si tiene Factura directa y está en el rango, agregarla
            if (pago.Factura != null && pago.Factura.MesFacturacion >= fechaInicio)
            {
                pagosMensuales.Add(new {
                    Ano = pago.Factura.MesFacturacion.Year,
                    Mes = pago.Factura.MesFacturacion.Month,
                    Categoria = pago.Factura.Categoria,
                    Monto = pago.Monto
                });
            }
            
            // Si tiene PagoFacturas, agregar cada una que esté en el rango
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.MesFacturacion >= fechaInicio)
                {
                    pagosMensuales.Add(new {
                        Ano = pagoFactura.Factura.MesFacturacion.Year,
                        Mes = pagoFactura.Factura.MesFacturacion.Month,
                        Categoria = pagoFactura.Factura.Categoria,
                        Monto = pagoFactura.MontoAplicado
                    });
                }
            }
        }
        
        // Agrupar y sumar
        var pagosMensualesAgrupados = pagosMensuales
            .GroupBy(p => new { ((dynamic)p).Ano, ((dynamic)p).Mes, ((dynamic)p).Categoria })
            .Select(g => new {
                Ano = g.Key.Ano,
                Mes = g.Key.Mes,
                Categoria = g.Key.Categoria,
                Total = g.Sum(p => (decimal)((dynamic)p).Monto)
            })
            .ToList();
        
        // Construir estadísticas mensuales desde los datos agrupados
        var estadisticasMensuales = new List<MesEstadistica>();
        for (int i = 5; i >= 0; i--)
        {
            var mes = fechaActual.AddMonths(-i);
            var mesStr = mes.ToString("MMM yyyy");
            
            var facturasInternetMes = facturasMensuales
                .FirstOrDefault(f => f.Ano == mes.Year && f.Mes == mes.Month && f.Categoria == SD.CategoriaInternet);
            
            var facturasStreamingMes = facturasMensuales
                .FirstOrDefault(f => f.Ano == mes.Year && f.Mes == mes.Month && f.Categoria == SD.CategoriaStreaming);
            
            var ingresosInternetMes = pagosMensualesAgrupados
                .FirstOrDefault(p => p.Ano == mes.Year && p.Mes == mes.Month && p.Categoria == SD.CategoriaInternet);
            
            var ingresosStreamingMes = pagosMensualesAgrupados
                .FirstOrDefault(p => p.Ano == mes.Year && p.Mes == mes.Month && p.Categoria == SD.CategoriaStreaming);
            
            estadisticasMensuales.Add(new MesEstadistica
            {
                Mes = mesStr,
                IngresosInternet = ingresosInternetMes?.Total ?? 0,
                IngresosStreaming = ingresosStreamingMes?.Total ?? 0,
                FacturasInternet = facturasInternetMes?.Count ?? 0,
                FacturasStreaming = facturasStreamingMes?.Count ?? 0
            });
        }

        var viewModel = new DashboardViewModel
        {
            // Estadísticas del mes actual
            IngresosMesActual = ingresosMesActual,
            EgresosMesActual = egresosMesActual,
            BalanceMesActual = balanceMesActual,
            FacturasMesActual = facturasMesActual,
            PagosMesActual = pagosMesActual.Count,
            MesActualTexto = mesActualTexto,
            
            // Estadísticas generales
            PagosPendientes = pagosPendientes,
            PagosRealizados = pagosRealizados,
            IngresoTotal = pagosRealizados,
            IngresoFaltante = pagosPendientes,
            TotalClientes = totalClientes,
            TotalFacturas = totalFacturas,
            TotalPagos = totalPagos,
            TotalClientesActivos = totalClientesActivos,
            
            // Internet (históricas)
            IngresosInternet = ingresosInternet,
            PendientesInternet = pendientesInternet,
            FacturasInternet = facturasInternetCount,
            FacturasPagadasInternet = facturasPagadasInternet,
            FacturasPendientesInternet = facturasPendientesInternet,
            
            // Internet (mes actual)
            IngresosMesActualInternet = ingresosMesActualInternet,
            FacturasPagadasMesActualInternet = facturasPagadasMesActualInternet,
            FacturasPendientesMesActualInternet = facturasPendientesMesActualInternet,
            FacturasMesActualInternet = facturasMesActualInternet,
            
            // Streaming (históricas)
            IngresosStreaming = ingresosStreaming,
            PendientesStreaming = pendientesStreaming,
            FacturasStreaming = facturasStreamingCount,
            FacturasPagadasStreaming = facturasPagadasStreaming,
            FacturasPendientesStreaming = facturasPendientesStreaming,
            
            // Streaming (mes actual)
            IngresosMesActualStreaming = ingresosMesActualStreaming,
            FacturasPagadasMesActualStreaming = facturasPagadasMesActualStreaming,
            FacturasPendientesMesActualStreaming = facturasPendientesMesActualStreaming,
            FacturasMesActualStreaming = facturasMesActualStreaming,
            
            // Clientes
            ClientesConInternet = clientesConInternet,
            ClientesConStreaming = clientesConStreaming,
            ClientesConAmbos = clientesConAmbos,
            
            // Egresos históricos
            TotalEgresos = _egresoService.CalcularTotalEgresos(),
            CantidadEgresos = _egresoService.ObtenerActivos().Count(),
            
            // Mensuales
            EstadisticasMensuales = estadisticasMensuales
        };

        return View(viewModel);
    }

    [HttpGet("/error")]
    [AllowAnonymous]
    public IActionResult Error(int? statusCode = null)
    {
        if (statusCode.HasValue)
        {
            ViewBag.StatusCode = statusCode.Value;
            ViewBag.ErrorMessage = statusCode.Value switch
            {
                404 => "Página no encontrada",
                403 => "Acceso denegado",
                500 => "Error interno del servidor",
                _ => "Ha ocurrido un error"
            };
        }
        else
        {
            ViewBag.StatusCode = 500;
            ViewBag.ErrorMessage = "Ha ocurrido un error";
        }

        return View();
    }
}


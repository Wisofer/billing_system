using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Data;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Controllers;

[Authorize]
[Route("[action]")]
public class HomeController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IPagoService _pagoService;
    private readonly IClienteService _clienteService;
    private readonly ApplicationDbContext _context;

    public HomeController(IFacturaService facturaService, IPagoService pagoService, IClienteService clienteService, ApplicationDbContext context)
    {
        _facturaService = facturaService;
        _pagoService = pagoService;
        _clienteService = clienteService;
        _context = context;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
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
        
        var ingresosInternet = _context.Pagos
            .Include(p => p.Factura)
            .Where(p => p.Factura != null && p.Factura.Categoria == SD.CategoriaInternet)
            .Sum(p => (decimal?)p.Monto) ?? 0;

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
        
        var ingresosStreaming = _context.Pagos
            .Include(p => p.Factura)
            .Where(p => p.Factura != null && p.Factura.Categoria == SD.CategoriaStreaming)
            .Sum(p => (decimal?)p.Monto) ?? 0;

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
        
        // Obtener todos los pagos de los últimos 6 meses en una sola consulta
        var pagosMensuales = _context.Pagos
            .Include(p => p.Factura)
            .Where(p => p.Factura != null && p.Factura.MesFacturacion >= fechaInicio)
            .GroupBy(p => new { 
                Ano = p.Factura.MesFacturacion.Year, 
                Mes = p.Factura.MesFacturacion.Month, 
                Categoria = p.Factura.Categoria 
            })
            .Select(g => new {
                g.Key.Ano,
                g.Key.Mes,
                g.Key.Categoria,
                Total = g.Sum(p => p.Monto)
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
            
            var ingresosInternetMes = pagosMensuales
                .FirstOrDefault(p => p.Ano == mes.Year && p.Mes == mes.Month && p.Categoria == SD.CategoriaInternet);
            
            var ingresosStreamingMes = pagosMensuales
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
            // Estadísticas generales
            PagosPendientes = pagosPendientes,
            PagosRealizados = pagosRealizados,
            IngresoTotal = pagosRealizados,
            IngresoFaltante = pagosPendientes,
            TotalClientes = totalClientes,
            TotalFacturas = totalFacturas,
            TotalPagos = totalPagos,
            TotalClientesActivos = totalClientesActivos,
            
            // Internet
            IngresosInternet = ingresosInternet,
            PendientesInternet = pendientesInternet,
            FacturasInternet = facturasInternetCount,
            FacturasPagadasInternet = facturasPagadasInternet,
            FacturasPendientesInternet = facturasPendientesInternet,
            
            // Streaming
            IngresosStreaming = ingresosStreaming,
            PendientesStreaming = pendientesStreaming,
            FacturasStreaming = facturasStreamingCount,
            FacturasPagadasStreaming = facturasPagadasStreaming,
            FacturasPendientesStreaming = facturasPendientesStreaming,
            
            // Clientes
            ClientesConInternet = clientesConInternet,
            ClientesConStreaming = clientesConStreaming,
            ClientesConAmbos = clientesConAmbos,
            
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


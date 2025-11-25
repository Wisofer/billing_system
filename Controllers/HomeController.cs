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
        // Estadísticas generales
        var pagosPendientes = _facturaService.CalcularTotalPendiente();
        var pagosRealizados = _pagoService.CalcularTotalIngresos();
        var totalClientes = _clienteService.ObtenerTotal();
        var totalFacturas = _facturaService.ObtenerTotal();
        var totalPagos = _pagoService.ObtenerTodos().Count;
        var totalClientesActivos = _clienteService.ObtenerTotalActivos();

        // Estadísticas por categoría - Internet
        var facturasInternet = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaInternet)
            .ToList();
        
        var ingresosInternet = _context.Pagos
            .Where(p => p.Factura != null && p.Factura.Categoria == SD.CategoriaInternet)
            .Sum(p => p.Monto);
        
        var pendientesInternet = facturasInternet
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);
        
        var facturasPagadasInternet = facturasInternet.Count(f => f.Estado == SD.EstadoFacturaPagada);
        var facturasPendientesInternet = facturasInternet.Count(f => f.Estado == SD.EstadoFacturaPendiente);

        // Estadísticas por categoría - Streaming
        var facturasStreaming = _context.Facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming)
            .ToList();
        
        var ingresosStreaming = _context.Pagos
            .Where(p => p.Factura != null && p.Factura.Categoria == SD.CategoriaStreaming)
            .Sum(p => p.Monto);
        
        var pendientesStreaming = facturasStreaming
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);
        
        var facturasPagadasStreaming = facturasStreaming.Count(f => f.Estado == SD.EstadoFacturaPagada);
        var facturasPendientesStreaming = facturasStreaming.Count(f => f.Estado == SD.EstadoFacturaPendiente);

        // Estadísticas de clientes por tipo de servicio
        var clientesConServicios = _context.Clientes
            .Include(c => c.ClienteServicios)
                .ThenInclude(cs => cs.Servicio)
            .Where(c => c.Activo && c.ClienteServicios.Any(cs => cs.Activo))
            .ToList();
        
        var clientesConInternet = clientesConServicios.Count(c => 
            c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaInternet));
        
        var clientesConStreaming = clientesConServicios.Count(c => 
            c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaStreaming));
        
        var clientesConAmbos = clientesConServicios.Count(c => 
            c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaInternet) &&
            c.ClienteServicios.Any(cs => cs.Activo && cs.Servicio != null && cs.Servicio.Categoria == SD.CategoriaStreaming));

        // Estadísticas mensuales (últimos 6 meses)
        var estadisticasMensuales = new List<MesEstadistica>();
        var fechaActual = DateTime.Now;
        
        for (int i = 5; i >= 0; i--)
        {
            var mes = fechaActual.AddMonths(-i);
            var mesStr = mes.ToString("MMM yyyy");
            
            var facturasMesInternet = _context.Facturas
                .Where(f => f.Categoria == SD.CategoriaInternet &&
                           f.MesFacturacion.Year == mes.Year &&
                           f.MesFacturacion.Month == mes.Month)
                .ToList();
            
            var facturasMesStreaming = _context.Facturas
                .Where(f => f.Categoria == SD.CategoriaStreaming &&
                           f.MesFacturacion.Year == mes.Year &&
                           f.MesFacturacion.Month == mes.Month)
                .ToList();
            
            var ingresosMesInternet = _context.Pagos
                .Where(p => p.Factura != null && 
                           p.Factura.Categoria == SD.CategoriaInternet &&
                           p.Factura.MesFacturacion.Year == mes.Year &&
                           p.Factura.MesFacturacion.Month == mes.Month)
                .Sum(p => (decimal?)p.Monto) ?? 0;
            
            var ingresosMesStreaming = _context.Pagos
                .Where(p => p.Factura != null && 
                           p.Factura.Categoria == SD.CategoriaStreaming &&
                           p.Factura.MesFacturacion.Year == mes.Year &&
                           p.Factura.MesFacturacion.Month == mes.Month)
                .Sum(p => (decimal?)p.Monto) ?? 0;
            
            estadisticasMensuales.Add(new MesEstadistica
            {
                Mes = mesStr,
                IngresosInternet = ingresosMesInternet,
                IngresosStreaming = ingresosMesStreaming,
                FacturasInternet = facturasMesInternet.Count,
                FacturasStreaming = facturasMesStreaming.Count
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
            FacturasInternet = facturasInternet.Count,
            FacturasPagadasInternet = facturasPagadasInternet,
            FacturasPendientesInternet = facturasPendientesInternet,
            
            // Streaming
            IngresosStreaming = ingresosStreaming,
            PendientesStreaming = pendientesStreaming,
            FacturasStreaming = facturasStreaming.Count,
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


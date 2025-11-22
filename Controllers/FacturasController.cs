using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers;

[Authorize]
[Route("[controller]/[action]")]
public class FacturasController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;
    private readonly IPdfService _pdfService;

    public FacturasController(IFacturaService facturaService, IClienteService clienteService, IServicioService servicioService, IPdfService pdfService)
    {
        _facturaService = facturaService;
        _clienteService = clienteService;
        _servicioService = servicioService;
        _pdfService = pdfService;
    }

    [HttpGet("/facturas")]
    public IActionResult Index(string? estado, int? mes, int? año, string? busquedaCliente, int pagina = 1, int tamanoPagina = 10)
    {
        var esAdministrador = SecurityHelper.IsAdministrator(User);

        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;

        // Obtener facturas con filtros
        var query = _facturaService.ObtenerTodas().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
        {
            query = query.Where(f => f.Estado == estado);
        }

        if (mes.HasValue && año.HasValue)
        {
            var fechaFiltro = new DateTime(año.Value, mes.Value, 1);
            query = query.Where(f => f.MesFacturacion.Year == fechaFiltro.Year && f.MesFacturacion.Month == fechaFiltro.Month);
        }

        if (!string.IsNullOrWhiteSpace(busquedaCliente))
        {
            var termino = busquedaCliente.ToLower();
            query = query.Where(f => 
                f.Cliente.Nombre.ToLower().Contains(termino) ||
                f.Cliente.Codigo.ToLower().Contains(termino));
        }

        var totalItems = query.Count();
        var facturas = query
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        // Estadísticas reales
        var totalFacturas = _facturaService.ObtenerTotal();
        var facturasPagadas = _facturaService.ObtenerTotalPagadas();
        var facturasPendientes = _facturaService.ObtenerTotalPendientes();
        var montoTotal = _facturaService.ObtenerMontoTotal();

        ViewBag.Estado = estado ?? "Todos";
        ViewBag.Mes = mes;
        ViewBag.Año = año ?? DateTime.Now.Year;
        ViewBag.BusquedaCliente = busquedaCliente;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / tamanoPagina);
        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.TotalFacturas = totalFacturas;
        ViewBag.FacturasPagadas = facturasPagadas;
        ViewBag.FacturasPendientes = facturasPendientes;
        ViewBag.MontoTotal = montoTotal;
        ViewBag.Clientes = _clienteService.ObtenerTodos();
        ViewBag.Servicios = _servicioService.ObtenerActivos();

        return View(facturas);
    }

    [HttpGet("/facturas/crear")]
    public IActionResult Crear()
    {
        ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        ViewBag.Servicios = _servicioService.ObtenerActivos();
        return View();
    }

    [HttpPost("/facturas/crear")]
    public IActionResult Crear([FromForm] int ClienteId, [FromForm] int ServicioId, [FromForm] string MesFacturacion)
    {
        var factura = new Factura
        {
            ClienteId = ClienteId,
            ServicioId = ServicioId
        };

        // Validar campos requeridos
        if (ClienteId == 0)
        {
            ModelState.AddModelError("ClienteId", "Debe seleccionar un cliente");
        }

        if (ServicioId == 0)
        {
            ModelState.AddModelError("ServicioId", "Debe seleccionar un servicio");
        }

        // Convertir el string del mes (YYYY-MM) a DateTime
        if (string.IsNullOrWhiteSpace(MesFacturacion))
        {
            ModelState.AddModelError("MesFacturacion", "Debe seleccionar un mes de facturación");
        }
        else
        {
            if (DateTime.TryParse(MesFacturacion + "-01", out var mesFecha))
            {
                factura.MesFacturacion = mesFecha;
            }
            else
            {
                ModelState.AddModelError("MesFacturacion", "El mes de facturación no es válido");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }

        try
        {
            _facturaService.Crear(factura);
            TempData["Success"] = "Factura creada exitosamente.";
            return Redirect("/facturas");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear factura: {ex.Message}";
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/facturas/generar-automaticas")]
    public IActionResult GenerarAutomaticas()
    {

        try
        {
            _facturaService.GenerarFacturasAutomaticas();
            TempData["Success"] = "Facturas automáticas generadas exitosamente.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar facturas: {ex.Message}";
            // Log del error para debugging
            System.Diagnostics.Debug.WriteLine($"Error al generar facturas automáticas: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return Redirect("/facturas");
    }

    [HttpGet("/facturas/ver/{id}")]
    public IActionResult Ver(int id)
    {
        var factura = _facturaService.ObtenerPorId(id);
        if (factura == null)
        {
            TempData["Error"] = "Factura no encontrada.";
            return Redirect("/facturas");
        }

        return View(factura);
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/facturas/editar/{id}")]
    public IActionResult Editar(int id)
    {

        var factura = _facturaService.ObtenerPorId(id);
        if (factura == null)
        {
            TempData["Error"] = "Factura no encontrada.";
            return Redirect("/facturas");
        }

        ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        ViewBag.Servicios = _servicioService.ObtenerActivos();
        return View(factura);
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/facturas/editar/{id}")]
    public IActionResult Editar(int id, [FromForm] string Estado, [FromForm] string? ArchivoPDF)
    {

        var factura = _facturaService.ObtenerPorId(id);
        if (factura == null)
        {
            TempData["Error"] = "Factura no encontrada.";
            return Redirect("/facturas");
        }

        // Validar estado
        if (string.IsNullOrWhiteSpace(Estado) || 
            (Estado != "Pendiente" && Estado != "Pagada" && Estado != "Cancelada"))
        {
            ModelState.AddModelError("Estado", "El estado debe ser Pendiente, Pagada o Cancelada");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }

        try
        {
            factura.Estado = Estado;
            if (!string.IsNullOrWhiteSpace(ArchivoPDF))
            {
                factura.ArchivoPDF = ArchivoPDF;
            }

            _facturaService.Actualizar(factura);
            TempData["Success"] = "Factura actualizada exitosamente.";
            return Redirect("/facturas");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar factura: {ex.Message}";
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
            ViewBag.Servicios = _servicioService.ObtenerActivos();
            return View(factura);
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/facturas/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {

        try
        {
            var eliminado = _facturaService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Factura eliminada exitosamente.";
            }
            else
            {
                TempData["Error"] = "No se puede eliminar la factura porque tiene pagos asociados.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar factura: {ex.Message}";
        }

        return Redirect("/facturas");
    }

    [HttpGet("/facturas/descargar-pdf/{id}")]
    public IActionResult DescargarPdf(int id)
    {
        var factura = _facturaService.ObtenerPorId(id);
        if (factura == null)
        {
            TempData["Error"] = "Factura no encontrada.";
            return Redirect("/facturas");
        }

        try
        {
            var pdfBytes = _pdfService.GenerarPdfFactura(factura);
            var nombreArchivo = $"Factura-{factura.Numero}-{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return Redirect("/facturas");
        }
    }
}

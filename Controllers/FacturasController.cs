using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

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

        // Aplicar filtro de mes y año
        // Si el usuario selecciona "Todos los meses" (mes viene como string vacío), no aplicar filtro
        // Si no viene el parámetro mes (primera carga), usar el mes actual por defecto
        var tieneParametroMes = Request.Query.ContainsKey("mes");
        var mesVacio = tieneParametroMes && string.IsNullOrEmpty(Request.Query["mes"].ToString());
        
        if (mesVacio)
        {
            // Usuario seleccionó "Todos los meses" - no aplicar filtro de mes
        }
        else
        {
            // Aplicar filtro: usar el mes especificado o el mes actual por defecto
            var mesFiltro = mes ?? DateTime.Now.Month;
            var añoFiltro = año ?? DateTime.Now.Year;
            var fechaFiltro = new DateTime(añoFiltro, mesFiltro, 1);
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
        // Si no se especifica un mes, usar el mes actual
        ViewBag.Mes = mes ?? DateTime.Now.Month;
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

    [HttpGet("/facturas/obtener-servicio-cliente/{clienteId}")]
    public IActionResult ObtenerServicioCliente(int clienteId)
    {
        var cliente = _clienteService.ObtenerPorId(clienteId);
        if (cliente == null)
        {
            return Json(new { servicioId = (int?)null });
        }

        return Json(new { servicioId = cliente.ServicioId });
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
            var estadoAnterior = factura.Estado;
            factura.Estado = Estado;
            
            if (!string.IsNullOrWhiteSpace(ArchivoPDF))
            {
                factura.ArchivoPDF = ArchivoPDF;
            }

            _facturaService.Actualizar(factura);

            // Sincronizar con sistema de pagos
            // Si cambias a "Pagada" y no hay pagos registrados, crear un pago automático
            if (Estado == SD.EstadoFacturaPagada && estadoAnterior != SD.EstadoFacturaPagada)
            {
                var pagoService = HttpContext.RequestServices.GetRequiredService<IPagoService>();
                var pagosExistentes = pagoService.ObtenerPorFactura(factura.Id);
                var totalPagado = pagosExistentes.Sum(p => p.Monto);

                // Solo crear pago si no hay pagos o el total pagado es menor al monto
                if (totalPagado < factura.Monto)
                {
                    var montoPendiente = factura.Monto - totalPagado;
                    var pagoAutomatico = new Pago
                    {
                        FacturaId = factura.Id,
                        Monto = montoPendiente,
                        Moneda = SD.MonedaCordoba,
                        TipoPago = SD.TipoPagoFisico, // Por defecto físico
                        FechaPago = DateTime.Now,
                        Observaciones = "Pago registrado automáticamente al cambiar estado de factura"
                    };
                    pagoService.Crear(pagoAutomatico);
                }
            }
            // Si cambias a "Pendiente" desde "Pagada", no eliminamos los pagos
            // porque puede ser un error y queremos mantener el historial

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

    [Authorize(Policy = "Administrador")]
    [HttpPost("/facturas/eliminar-multiples")]
    public IActionResult EliminarMultiples([FromForm] List<int> facturaIds)
    {
        if (facturaIds == null || !facturaIds.Any())
        {
            TempData["Error"] = "No se seleccionaron facturas para eliminar.";
            return Redirect("/facturas");
        }

        try
        {
            var resultado = _facturaService.EliminarMultiples(facturaIds);
            
            var mensajes = new List<string>();
            if (resultado.eliminadas > 0)
            {
                mensajes.Add($"{resultado.eliminadas} factura(s) eliminada(s) exitosamente.");
            }
            if (resultado.conPagos > 0)
            {
                mensajes.Add($"{resultado.conPagos} factura(s) no se pudieron eliminar porque tienen pagos asociados.");
            }
            if (resultado.noEncontradas > 0)
            {
                mensajes.Add($"{resultado.noEncontradas} factura(s) no encontrada(s).");
            }

            if (mensajes.Any())
            {
                if (resultado.eliminadas > 0)
                {
                    TempData["Success"] = string.Join(" ", mensajes);
                }
                else
                {
                    TempData["Error"] = string.Join(" ", mensajes);
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar facturas: {ex.Message}";
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

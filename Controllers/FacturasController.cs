using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace billing_system.Controllers;

[Authorize(Policy = "FacturasPagos")]
[Route("[controller]/[action]")]
public class FacturasController : Controller
{
    private readonly IFacturaService _facturaService;
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;
    private readonly IPdfService _pdfService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;

    public FacturasController(IFacturaService facturaService, IClienteService clienteService, IServicioService servicioService, IPdfService pdfService, IWhatsAppService whatsAppService, IConfiguration configuration)
    {
        _facturaService = facturaService;
        _clienteService = clienteService;
        _servicioService = servicioService;
        _pdfService = pdfService;
        _whatsAppService = whatsAppService;
        _configuration = configuration;
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
        ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
        return View();
    }

    [HttpGet("/facturas/obtener-servicio-cliente/{clienteId}")]
    public IActionResult ObtenerServicioCliente(int clienteId)
    {
        var cliente = _clienteService.ObtenerPorId(clienteId);
        if (cliente == null)
        {
            return Json(new { servicioId = (int?)null, serviciosActivos = new List<object>() });
        }

        // Obtener servicios activos del cliente desde ClienteServicios
        var serviciosActivos = _clienteService.ObtenerServiciosActivos(clienteId)
            .Where(cs => cs.Servicio != null && cs.Servicio.Activo) // Asegurar que el servicio esté activo
            .Select(cs => new { 
                id = cs.ServicioId, 
                nombre = cs.Servicio!.Nombre,
                precio = cs.Servicio.Precio,
                categoria = cs.Servicio.Categoria,
                cantidad = cs.Cantidad // Incluir la cantidad de suscripciones
            })
            .ToList();

        // Si no hay servicios en ClienteServicios pero el cliente tiene un ServicioId (compatibilidad)
        if (!serviciosActivos.Any() && cliente.ServicioId.HasValue)
        {
            var servicio = _servicioService.ObtenerPorId(cliente.ServicioId.Value);
            if (servicio != null && servicio.Activo)
            {
                serviciosActivos.Add(new { 
                    id = servicio.Id, 
                    nombre = servicio.Nombre,
                    precio = servicio.Precio,
                    categoria = servicio.Categoria,
                    cantidad = 1 // Default para compatibilidad
                });
            }
        }

        return Json(new { 
            servicioId = cliente.ServicioId, // Mantener para compatibilidad
            serviciosActivos = serviciosActivos 
        });
    }

    [HttpPost("/facturas/crear")]
    public IActionResult Crear([FromForm] int ClienteId, [FromForm] string MesFacturacion)
    {
        // Obtener servicios seleccionados
        var servicioIds = Request.Form["ServicioIds"]
            .Where(id => int.TryParse(id.ToString(), out _))
            .Select(id => int.Parse(id.ToString()))
            .ToList();

        // Validar campos requeridos
        if (ClienteId == 0)
        {
            ModelState.AddModelError("ClienteId", "Debe seleccionar un cliente");
        }

        if (!servicioIds.Any())
        {
            ModelState.AddModelError("ServicioIds", "Debe seleccionar al menos un servicio");
        }

        // Convertir el string del mes (YYYY-MM) a DateTime
        DateTime mesFecha = DateTime.Now;
        if (string.IsNullOrWhiteSpace(MesFacturacion))
        {
            ModelState.AddModelError("MesFacturacion", "Debe seleccionar un mes de facturación");
        }
        else
        {
            if (!DateTime.TryParse(MesFacturacion + "-01", out mesFecha))
            {
                ModelState.AddModelError("MesFacturacion", "El mes de facturación no es válido");
            }
        }

        if (!ModelState.IsValid)
        {
            ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
            return View(new Factura { ClienteId = ClienteId });
        }

        try
        {
            // Crear facturas agrupadas por categoría
            var facturasCreadas = _facturaService.CrearFacturasAgrupadasPorCategoria(ClienteId, servicioIds, mesFecha);

            if (facturasCreadas.Any())
            {
                var categorias = facturasCreadas.Select(f => f.Categoria).Distinct().ToList();
                TempData["Success"] = $"{facturasCreadas.Count} factura(s) creada(s) exitosamente " +
                    $"(Internet: {categorias.Count(c => c == SD.CategoriaInternet)}, " +
                    $"Streaming: {categorias.Count(c => c == SD.CategoriaStreaming)}).";
            }
            else
            {
                TempData["Warning"] = "No se pudieron crear facturas. Puede que ya existan facturas para los servicios seleccionados en este mes.";
            }

            return Redirect("/facturas");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear facturas: {ex.Message}";
            ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
            return View(new Factura { ClienteId = ClienteId });
        }
    }

    [Authorize(Policy = "Administrador")]
    [HttpGet("/facturas/contar-clientes-generar")]
    public IActionResult ContarClientesGenerar()
    {
        try
        {
            var total = _clienteService.ObtenerTodos()
                .Count(c => c.Activo && c.ServicioId.HasValue);
            return Json(new { total });
        }
        catch (Exception ex)
        {
            return Json(new { total = 0, error = ex.Message });
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

        ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
        return View(factura);
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/facturas/editar/{id}")]
    [ValidateAntiForgeryToken]
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
            ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
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
            ControllerHelper.SetClientesYServicios(ViewBag, _clienteService, _servicioService);
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
            
            // Configurar headers para compatibilidad con móviles
            // Forzar descarga (attachment) en lugar de abrir en el navegador (inline)
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{nombreArchivo}\"; filename*=UTF-8''{Uri.EscapeDataString(nombreArchivo)}");
            Response.Headers.Append("Content-Type", "application/pdf");
            Response.Headers.Append("Content-Length", pdfBytes.Length.ToString());
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return Redirect("/facturas");
        }
    }

    /// <summary>
    /// Ruta pública para descargar PDFs sin autenticación (usada para compartir por WhatsApp)
    /// Requiere un token válido para seguridad
    /// </summary>
    [AllowAnonymous]
    [HttpGet("/facturas/descargar-pdf-publico/{id}")]
    public IActionResult DescargarPdfPublico(int id, [FromQuery] string? token)
    {
        // Validar token
        if (string.IsNullOrWhiteSpace(token) || !PdfTokenHelper.ValidarToken(id, token, _configuration))
        {
            return Unauthorized("Token inválido o faltante. No tiene permiso para acceder a este recurso.");
        }

        var factura = _facturaService.ObtenerPorId(id);
        if (factura == null)
        {
            return NotFound("Factura no encontrada.");
        }

        try
        {
            var pdfBytes = _pdfService.GenerarPdfFactura(factura);
            var nombreArchivo = $"Factura-{factura.Numero}-{DateTime.Now:yyyyMMdd}.pdf";
            
            // Configurar headers para descarga directa
            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{nombreArchivo}\"");
            
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al generar PDF: {ex.Message}");
        }
    }

    [HttpGet("/facturas/obtener-ids-facturas")]
    [Produces("application/json")]
    public IActionResult ObtenerIdsFacturas(string? estado, int? mes, int? año, string? busquedaCliente)
    {
        try
        {
            // Aplicar los mismos filtros que en Index
            var query = _facturaService.ObtenerTodas().AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                query = query.Where(f => f.Estado == estado);
            }

            // Aplicar filtro de mes y año
            var tieneParametroMes = Request.Query.ContainsKey("mes");
            var mesVacio = tieneParametroMes && string.IsNullOrEmpty(Request.Query["mes"].ToString());
            
            if (!mesVacio)
            {
                var mesFiltro = mes ?? DateTime.Now.Month;
                var añoFiltro = año ?? DateTime.Now.Year;
                var fechaFiltro = new DateTime(añoFiltro, mesFiltro, 1);
                query = query.Where(f => f.MesFacturacion.Year == fechaFiltro.Year && f.MesFacturacion.Month == fechaFiltro.Month);
            }

            if (!string.IsNullOrWhiteSpace(busquedaCliente))
            {
                var termino = busquedaCliente.ToLower();
                // Materializar la consulta para poder acceder a las propiedades de navegación
                var facturas = query.ToList();
                facturas = facturas.Where(f => 
                    (f.Cliente?.Nombre?.ToLower().Contains(termino) ?? false) ||
                    (f.Cliente?.Codigo?.ToLower().Contains(termino) ?? false)).ToList();
                var facturasIds = facturas.OrderBy(f => f.Numero).Select(f => f.Id).Distinct().ToList();
                return Json(new { ids = facturasIds, total = facturasIds.Count });
            }

            var facturasIdsFinal = query.OrderBy(f => f.Numero).Select(f => f.Id).Distinct().ToList();

            return Json(new { ids = facturasIdsFinal, total = facturasIdsFinal.Count });
        }
        catch (Exception ex)
        {
            // Log del error (podrías usar un logger aquí)
            return StatusCode(500, Json(new { error = ex.Message, ids = new List<int>(), total = 0 }));
        }
    }

    [HttpGet("/facturas/whatsapp/{id}")]
    public IActionResult EnviarWhatsApp(int id)
    {
        var factura = _facturaService.ObtenerPorId(id);
        
        if (factura == null)
        {
            TempData["Error"] = "Factura no encontrada";
            return RedirectToAction("Index");
        }

        // Verificar que el cliente tenga teléfono
        if (string.IsNullOrWhiteSpace(factura.Cliente?.Telefono))
        {
            TempData["Error"] = "El cliente no tiene número de teléfono registrado";
            return RedirectToAction("Index");
        }

        // Generar URL base para el PDF
        var urlBase = $"{Request.Scheme}://{Request.Host}";
        
        // Obtener la plantilla por defecto
        var plantilla = _whatsAppService.ObtenerPlantillaDefault();
        
        string mensaje;
        if (plantilla == null)
        {
            // Generar token seguro para el PDF
            var token = PdfTokenHelper.GenerarToken(factura.Id, _configuration);
            var enlacePDFCompleto = $"{urlBase}/facturas/descargar-pdf-publico/{factura.Id}?token={token}";
            
            // Si no hay plantilla, usar una por defecto
            mensaje = $"Hola {factura.Cliente?.Nombre ?? "Cliente"},\n\n" +
                     $"Le enviamos su factura:\n" +
                     $"📄 Factura: {factura.Numero}\n" +
                     $"💰 Monto: C$ {factura.Monto:N2}\n" +
                     $"📅 Mes: {factura.MesFacturacion:MMMM yyyy}\n" +
                     $"🔗 Descargar PDF: {enlacePDFCompleto}\n\n" +
                     $"Gracias por su preferencia.";
        }
        else
        {
            // Generar mensaje con la plantilla (ya incluye el token en el enlace)
            mensaje = _whatsAppService.GenerarMensaje(factura, plantilla.Mensaje, urlBase);
        }

        // Formatear número de teléfono
        var numeroTelefono = _whatsAppService.FormatearNumeroWhatsApp(factura.Cliente?.Telefono);
        
        // Generar enlace de WhatsApp y redirigir directamente
        var enlaceWhatsApp = _whatsAppService.GenerarEnlaceWhatsApp(numeroTelefono, mensaje);
        
        return Redirect(enlaceWhatsApp);
    }
}

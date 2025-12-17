using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Globalization;
using OfficeOpenXml;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Pagos")]
[Route("[controller]/[action]")]
public class PagosController : Controller
{
    private readonly IPagoService _pagoService;
    private readonly IFacturaService _facturaService;
    private readonly IClienteService _clienteService;
    private readonly IConfiguracionService _configuracionService;

    public PagosController(IPagoService pagoService, IFacturaService facturaService, IClienteService clienteService, IConfiguracionService configuracionService)
    {
        _pagoService = pagoService;
        _facturaService = facturaService;
        _clienteService = clienteService;
        _configuracionService = configuracionService;
    }

    [HttpGet("/pagos")]
    public IActionResult Index(string? tipoPago, string? banco, DateTime? fecha, int pagina = 1, int tamanoPagina = 25)
    {
        var esAdministrador = SecurityHelper.IsAdministrator(User);
        
        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;
        
        var pagos = _pagoService.ObtenerTodos().AsQueryable();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(tipoPago) && tipoPago != "Todos")
        {
            pagos = pagos.Where(p => p.TipoPago == tipoPago);
        }

        if (!string.IsNullOrWhiteSpace(banco) && banco != "Todos")
        {
            pagos = pagos.Where(p => p.Banco == banco);
        }

        if (fecha.HasValue)
        {
            pagos = pagos.Where(p => p.FechaPago.Date == fecha.Value.Date);
        }

        // Aplicar paginación
        var totalItems = pagos.Count();
        var pagosLista = pagos
            .OrderByDescending(p => p.FechaPago)
            .ThenByDescending(p => p.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        // Estadísticas (optimizado: calcular una sola vez)
        var todosPagos = _pagoService.ObtenerTodos();
        var totalPagos = todosPagos.Count;
        var montoTotal = _pagoService.CalcularTotalIngresos();
        var pagosFisicos = todosPagos.Count(p => p.TipoPago == SD.TipoPagoFisico);
        var pagosElectronicos = todosPagos.Count(p => p.TipoPago == SD.TipoPagoElectronico);

        ViewBag.TotalPagos = totalPagos;
        ViewBag.MontoTotal = montoTotal;
        ViewBag.PagosFisicos = pagosFisicos;
        ViewBag.PagosElectronicos = pagosElectronicos;
        ViewBag.TipoPago = tipoPago ?? "Todos";
        ViewBag.Banco = banco ?? "Todos";
        ViewBag.Fecha = fecha;
        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / tamanoPagina);

        return View(pagosLista);
    }

    [HttpGet("/pagos/crear")]
    public IActionResult Crear(string? clienteBusqueda)
    {
        ViewBag.ClienteBusqueda = clienteBusqueda;
        // Cargar todos los clientes para el buscador
        ViewBag.TodosLosClientes = _clienteService.ObtenerTodos();
        ViewBag.Clientes = string.IsNullOrWhiteSpace(clienteBusqueda)
            ? new List<Cliente>()
            : _clienteService.Buscar(clienteBusqueda);

        ViewBag.Facturas = new List<Factura>();
        // Tipos de cambio: Compra (cliente paga en $) y Venta (mostrar equivalentes)
        var tipoCambioCompra = _configuracionService.ObtenerValorDecimal("TipoCambioCompra") ?? SD.TipoCambioCompra;
        var tipoCambioVenta = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
        ViewBag.TipoCambio = tipoCambioCompra; // Usar Compra para pagos
        ViewBag.TipoCambioCompra = tipoCambioCompra;
        ViewBag.TipoCambioVenta = tipoCambioVenta;
        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
        ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };

        return View();
    }

    [HttpPost("/pagos/buscar-cliente")]
    public IActionResult BuscarCliente(string clienteBusqueda)
    {
        return RedirectToAction("Crear", new { clienteBusqueda });
    }

    [HttpGet("/pagos/facturas-cliente/{clienteId}")]
    public IActionResult FacturasCliente(int clienteId)
    {
        // Obtener todas las facturas del cliente (pendientes y pagadas)
        // Esto permite registrar pagos adicionales si es necesario
        var facturas = _facturaService.ObtenerPorCliente(clienteId)
            .OrderByDescending(f => f.MesFacturacion)
            .ThenByDescending(f => f.FechaCreacion)
            .ToList();
        
        // Calcular el total pagado para cada factura
        var facturasConPago = facturas.Select(f => 
        {
            var totalPagado = _pagoService.ObtenerPorFactura(f.Id).Sum(p => p.Monto);
            var saldoPendiente = f.Monto - totalPagado;
            
            // Si el saldo pendiente es negativo o cero, no se puede pagar
            // Pero si es negativo, significa que se pagó de más, así que el saldo pendiente debe ser 0
            var saldoPendienteAjustado = saldoPendiente < 0 ? 0 : saldoPendiente;
            
            return new 
            { 
                f.Id, 
                f.Numero, 
                Monto = f.Monto, // Asegurar que sea un número, no string formateado
                MesFacturacion = f.MesFacturacion.ToString("yyyy-MM-dd"),
                f.Estado,
                TotalPagado = totalPagado, // Asegurar que sea un número
                SaldoPendiente = saldoPendienteAjustado, // Usar el saldo ajustado
                PuedePagar = saldoPendienteAjustado > 0 // Solo se puede pagar si hay saldo pendiente
            };
        }).ToList();
        
        return Json(facturasConPago);
    }

    [HttpPost("/pagos/crear")]
    public IActionResult Crear([FromForm] Pago pago)
    {
        // Log para debugging (remover en producción)
        var logger = HttpContext.RequestServices.GetService<ILogger<PagosController>>();
        
        // Normalizar montos provenientes del formulario (manejo de coma como separador decimal)
        decimal? ParseDecimalFromForm(string key)
        {
            var raw = Request.Form[key].ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            // Reemplazar coma por punto. No eliminamos puntos porque no usamos separador de miles en los inputs.
            var normalized = raw.Trim().Replace(",", ".");

            if (decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            logger?.LogWarning($"No se pudo parsear el valor decimal del campo '{key}' con valor crudo '{raw}'");
            return null;
        }

        // Aplicar normalización a los campos relevantes de pago
        var montoForm = ParseDecimalFromForm("Monto");
        if (montoForm.HasValue)
        {
            pago.Monto = montoForm.Value;
        }

        pago.MontoCordobasFisico = ParseDecimalFromForm("MontoCordobasFisico") ?? pago.MontoCordobasFisico;
        pago.MontoDolaresFisico = ParseDecimalFromForm("MontoDolaresFisico") ?? pago.MontoDolaresFisico;
        
        // Manejar MontoRecibidoFisico: si la moneda es "Ambos", buscar campos separados
        if (pago.Moneda == "Ambos")
        {
            // Si la moneda es "Ambos", buscar campos separados para recibido en ambas monedas
            var montoRecibidoCordobasFisico = ParseDecimalFromForm("MontoRecibidoCordobasFisico");
            var montoRecibidoDolaresFisico = ParseDecimalFromForm("MontoRecibidoDolaresFisico");
            
            if (montoRecibidoCordobasFisico.HasValue || montoRecibidoDolaresFisico.HasValue)
            {
                // Usar TipoCambioCompra para pagos en dólares del cliente
                var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioCompra") ?? SD.TipoCambioCompra;
                
                // Calcular el total recibido en córdobas: córdobas + (dólares * tipo de cambio compra)
                var totalRecibido = (montoRecibidoCordobasFisico ?? 0) + ((montoRecibidoDolaresFisico ?? 0) * tipoCambio);
                pago.MontoRecibidoFisico = totalRecibido;
                
                logger?.LogInformation($"Pago con moneda 'Ambos': Recibido C$ {montoRecibidoCordobasFisico ?? 0} + $ {montoRecibidoDolaresFisico ?? 0} = Total C$ {totalRecibido}");
            }
            else
            {
                // Si no hay campos separados, usar el campo único (compatibilidad)
                pago.MontoRecibidoFisico = ParseDecimalFromForm("MontoRecibidoFisico") ?? pago.MontoRecibidoFisico;
            }
        }
        else
        {
            // Si la moneda es C$ o $, usar el campo único como siempre
            pago.MontoRecibidoFisico = ParseDecimalFromForm("MontoRecibidoFisico") ?? pago.MontoRecibidoFisico;
        }
        
        pago.MontoCordobasElectronico = ParseDecimalFromForm("MontoCordobasElectronico") ?? pago.MontoCordobasElectronico;
        pago.MontoDolaresElectronico = ParseDecimalFromForm("MontoDolaresElectronico") ?? pago.MontoDolaresElectronico;

        // Log para debugging (remover en producción)
        var montoRaw = Request.Form["Monto"].ToString();
        logger?.LogInformation($"=== DEBUG PAGO ===");
        logger?.LogInformation($"Monto crudo del formulario: '{montoRaw}'");
        logger?.LogInformation($"Monto después de normalización: {pago.Monto}");
        logger?.LogInformation($"MontoCordobasFisico: {pago.MontoCordobasFisico}, MontoDolaresFisico: {pago.MontoDolaresFisico}");
        logger?.LogInformation($"MontoCordobasElectronico: {pago.MontoCordobasElectronico}, MontoDolaresElectronico: {pago.MontoDolaresElectronico}");
        logger?.LogInformation($"FacturaId: {pago.FacturaId}, TipoPago: {pago.TipoPago}");

        // Obtener facturas seleccionadas (puede ser una o múltiples)
        var facturaIds = Request.Form["FacturaIds"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.TryParse(id, out var parsedId) ? parsedId : 0)
            .Where(id => id > 0)
            .ToList();

        // Remover errores de validación de propiedades de navegación (relaciones)
        ModelState.Remove("Factura");
        ModelState.Remove("PagoFacturas");
        
        // Validación manual de campos requeridos
        if (facturaIds.Count == 0 && (!pago.FacturaId.HasValue || pago.FacturaId.Value == 0))
        {
            ModelState.AddModelError("FacturaId", "Debe seleccionar al menos una factura");
        }

        if (string.IsNullOrWhiteSpace(pago.TipoPago))
        {
            ModelState.AddModelError("TipoPago", "Debe seleccionar un tipo de pago");
        }

        if (pago.Monto <= 0)
        {
            ModelState.AddModelError("Monto", "El monto debe ser mayor a cero");
        }

        // VALIDACIÓN CRÍTICA: Verificar que el monto no sea excesivamente alto
        if (facturaIds.Count > 0)
        {
            // Validar múltiples facturas
            var facturas = facturaIds.Select(id => _facturaService.ObtenerPorId(id))
                .Where(f => f != null)
                .ToList();

            if (facturas.Count != facturaIds.Count)
            {
                ModelState.AddModelError("FacturaId", "Una o más facturas no fueron encontradas");
            }
            else if (facturas.Count > 0)
            {
                // Verificar que todas pertenezcan al mismo cliente
                var primerClienteId = facturas.First()!.ClienteId;
                if (facturas.Any(f => f!.ClienteId != primerClienteId))
                {
                    ModelState.AddModelError("FacturaId", "Todas las facturas deben pertenecer al mismo cliente");
                }
                else
                {
                    // Calcular saldo total pendiente
                    var saldosPendientes = facturas.Select(f =>
                    {
                        var totalPagado = _pagoService.ObtenerPorFactura(f!.Id).Sum(p => p.Monto);
                        return f.Monto - totalPagado;
                    }).ToList();

                    var saldoTotalPendiente = saldosPendientes.Sum();
                    var saldoTotalAjustado = saldoTotalPendiente < 0 ? 0 : saldoTotalPendiente;
                    
                    logger?.LogInformation($"Facturas seleccionadas: {facturas.Count}, Saldo Total Pendiente: {saldoTotalAjustado}, Monto Pago: {pago.Monto}");
                    
                    // Validación suave: solo registrar advertencia si el monto parece extremadamente alto,
                    // pero sin bloquear el registro del pago para evitar falsos positivos con pagos mixtos o ajustes manuales.
                    if (pago.Monto > saldoTotalAjustado * 10m) // más de 10 veces el saldo pendiente
                    {
                        logger?.LogWarning($"[VALIDACIÓN SUAVE] Monto de pago alto: {pago.Monto} vs saldo pendiente total: {saldoTotalAjustado}");
                        // No agregamos ModelState error para no impedir el registro del pago.
                    }
                }
            }
        }
        else if (pago.FacturaId.HasValue && pago.FacturaId.Value > 0)
        {
            // Validar una sola factura (comportamiento original)
            var factura = _facturaService.ObtenerPorId(pago.FacturaId.Value);
            if (factura != null)
            {
                // Obtener el saldo pendiente real
                var totalPagado = _pagoService.ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
                var saldoPendiente = factura.Monto - totalPagado;
                var saldoPendienteAjustado = saldoPendiente < 0 ? 0 : saldoPendiente;
                
                logger?.LogInformation($"Factura: {factura.Numero}, Monto Factura: {factura.Monto}, Total Pagado: {totalPagado}, Saldo Pendiente: {saldoPendienteAjustado}, Monto Pago: {pago.Monto}");
                
                // Validar que el monto del pago no exceda el saldo pendiente por más del 10%
                if (pago.Monto > saldoPendienteAjustado * 1.1m)
                {
                    ModelState.AddModelError("Monto", $"El monto del pago (C$ {pago.Monto:N2}) excede significativamente el saldo pendiente (C$ {saldoPendienteAjustado:N2}). Por favor, verifica el monto.");
                    logger?.LogWarning($"Monto de pago sospechoso: {pago.Monto} vs saldo pendiente: {saldoPendienteAjustado}");
                }
                
                // Validar que el monto no sea más de 10 veces el monto de la factura
                if (pago.Monto > factura.Monto * 10m)
                {
                    ModelState.AddModelError("Monto", $"El monto del pago (C$ {pago.Monto:N2}) es demasiado alto comparado con el monto de la factura (C$ {factura.Monto:N2}). Por favor, verifica el monto.");
                    logger?.LogWarning($"Monto de pago excesivo: {pago.Monto} vs monto factura: {factura.Monto}");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(pago.Moneda))
        {
            ModelState.AddModelError("Moneda", "Debe seleccionar una moneda");
        }

        // Validaciones para pagos electrónicos y mixtos
        if (pago.TipoPago == SD.TipoPagoElectronico || pago.TipoPago == SD.TipoPagoMixto)
        {
            // Validar que haya al menos un monto electrónico
            var tieneMontoElectronico = (pago.MontoCordobasElectronico.HasValue && pago.MontoCordobasElectronico.Value > 0) ||
                                       (pago.MontoDolaresElectronico.HasValue && pago.MontoDolaresElectronico.Value > 0);
            
            if (pago.TipoPago == SD.TipoPagoElectronico && !tieneMontoElectronico && pago.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "Debe especificar un monto para el pago electrónico");
            }
            
            if (tieneMontoElectronico)
            {
                if (string.IsNullOrWhiteSpace(pago.Banco))
                {
                    ModelState.AddModelError("Banco", "El banco es requerido para pagos electrónicos");
                }
                if (string.IsNullOrWhiteSpace(pago.TipoCuenta))
                {
                    ModelState.AddModelError("TipoCuenta", "El tipo de cuenta es requerido para pagos electrónicos");
                }
            }
        }
        
        // Validaciones para pagos físicos y mixtos
        if (pago.TipoPago == SD.TipoPagoFisico || pago.TipoPago == SD.TipoPagoMixto)
        {
            // Validar que haya al menos un monto físico
            var tieneMontoFisico = (pago.MontoCordobasFisico.HasValue && pago.MontoCordobasFisico.Value > 0) ||
                                  (pago.MontoDolaresFisico.HasValue && pago.MontoDolaresFisico.Value > 0);
            
            if (pago.TipoPago == SD.TipoPagoFisico && !tieneMontoFisico && pago.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "Debe especificar un monto para el pago físico");
            }
        }
        
        // Validación para pago mixto: debe tener montos en ambos métodos
        if (pago.TipoPago == SD.TipoPagoMixto)
        {
            var tieneMontoFisico = (pago.MontoCordobasFisico.HasValue && pago.MontoCordobasFisico.Value > 0) ||
                                  (pago.MontoDolaresFisico.HasValue && pago.MontoDolaresFisico.Value > 0);
            var tieneMontoElectronico = (pago.MontoCordobasElectronico.HasValue && pago.MontoCordobasElectronico.Value > 0) ||
                                       (pago.MontoDolaresElectronico.HasValue && pago.MontoDolaresElectronico.Value > 0);
            
            if (!tieneMontoFisico)
            {
                ModelState.AddModelError("MontoCordobasFisico", "Debe especificar al menos un monto para el pago físico");
            }
            if (!tieneMontoElectronico)
            {
                ModelState.AddModelError("MontoCordobasElectronico", "Debe especificar al menos un monto para el pago electrónico");
            }
        }

        // Calcular monto total antes de validar
        var montoTotalCalculado = _pagoService.CalcularMontoTotal(pago);
        pago.Monto = montoTotalCalculado;
        
        // Establecer Moneda según el tipo de pago
        if (pago.TipoPago == SD.TipoPagoMixto)
        {
            pago.Moneda = SD.MonedaAmbos;
        }
        else if (pago.TipoPago == SD.TipoPagoFisico)
        {
            // Si usa nuevos campos, determinar moneda
            if (pago.MontoCordobasFisico.HasValue && pago.MontoCordobasFisico.Value > 0 &&
                pago.MontoDolaresFisico.HasValue && pago.MontoDolaresFisico.Value > 0)
            {
                pago.Moneda = SD.MonedaAmbos;
            }
            else if (pago.MontoDolaresFisico.HasValue && pago.MontoDolaresFisico.Value > 0)
            {
                pago.Moneda = SD.MonedaDolar;
            }
            else
            {
                pago.Moneda = SD.MonedaCordoba;
            }
        }
        else if (pago.TipoPago == SD.TipoPagoElectronico)
        {
            // Si usa nuevos campos, determinar moneda
            if (pago.MontoCordobasElectronico.HasValue && pago.MontoCordobasElectronico.Value > 0 &&
                pago.MontoDolaresElectronico.HasValue && pago.MontoDolaresElectronico.Value > 0)
            {
                pago.Moneda = SD.MonedaAmbos;
            }
            else if (pago.MontoDolaresElectronico.HasValue && pago.MontoDolaresElectronico.Value > 0)
            {
                pago.Moneda = SD.MonedaDolar;
            }
            else
            {
                pago.Moneda = SD.MonedaCordoba;
            }
        }
        
        if (montoTotalCalculado <= 0)
        {
            ModelState.AddModelError("Monto", "El monto total del pago debe ser mayor a cero");
        }
        
        // Validar solo los campos que realmente importan
        var isValid = ModelState.IsValid && 
                     (facturaIds.Count > 0 || (pago.FacturaId.HasValue && pago.FacturaId.Value > 0)) && 
                     montoTotalCalculado > 0 && 
                     !string.IsNullOrWhiteSpace(pago.TipoPago) &&
                     !string.IsNullOrWhiteSpace(pago.Moneda);

        if (!isValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(e => !e.Contains("Factura") && !e.Contains("PagoFacturas")) // Filtrar errores de relaciones
                .ToList();
            
            logger?.LogWarning($"Errores de validación: {string.Join(", ", errors)}");
            
            var tipoCambioCompra = _configuracionService.ObtenerValorDecimal("TipoCambioCompra") ?? SD.TipoCambioCompra;
            var tipoCambioVenta = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
            ViewBag.TipoCambio = tipoCambioCompra;
            ViewBag.TipoCambioCompra = tipoCambioCompra;
            ViewBag.TipoCambioVenta = tipoCambioVenta;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            ViewBag.ClienteBusqueda = "";
            ViewBag.Clientes = new List<Cliente>();
            ViewBag.Facturas = new List<Factura>();
            
            // Mostrar errores de validación
            if (errors.Any())
            {
                TempData["Error"] = $"Errores de validación: {string.Join(", ", errors)}";
            }
            return View(pago);
        }

        try
        {
            // Si hay múltiples facturas, usar el nuevo método
            if (facturaIds.Count > 0)
            {
                _pagoService.Crear(pago, facturaIds);
            }
            else
            {
                // Pago de una sola factura (comportamiento original)
                _pagoService.Crear(pago);
            }
            
            TempData["Success"] = facturaIds.Count > 1 
                ? $"Pago registrado exitosamente para {facturaIds.Count} facturas." 
                : "Pago registrado exitosamente.";
            return Redirect("/pagos");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error al crear pago");
            TempData["Error"] = $"Error al registrar pago: {ex.Message}";
            var tipoCambioCompra = _configuracionService.ObtenerValorDecimal("TipoCambioCompra") ?? SD.TipoCambioCompra;
            var tipoCambioVenta = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
            ViewBag.TipoCambio = tipoCambioCompra;
            ViewBag.TipoCambioCompra = tipoCambioCompra;
            ViewBag.TipoCambioVenta = tipoCambioVenta;
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF };
            ViewBag.TiposCuenta = new[] { SD.TipoCuentaDolar, SD.TipoCuentaCordoba, SD.TipoCuentaBilletera };
            ViewBag.ClienteBusqueda = "";
            ViewBag.Clientes = new List<Cliente>();
            ViewBag.Facturas = new List<Factura>();
            return View(pago);
        }
    }

    [HttpPost("/pagos/eliminar/{id}")]
    public IActionResult Eliminar(int id)
    {

        try
        {
            var eliminado = _pagoService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Pago eliminado exitosamente.";
            }
            else
            {
                TempData["Error"] = "Error al eliminar el pago.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar pago: {ex.Message}";
        }

        return Redirect("/pagos");
    }

    [Authorize(Policy = "Administrador")]
    [HttpPost("/pagos/eliminar-multiples")]
    public IActionResult EliminarMultiples([FromForm] List<int> pagoIds)
    {
        if (pagoIds == null || !pagoIds.Any())
        {
            TempData["Error"] = "No se seleccionaron pagos para eliminar.";
            return Redirect("/pagos");
        }

        try
        {
            var resultado = _pagoService.EliminarMultiples(pagoIds);
            
            var mensajes = new List<string>();
            if (resultado.eliminados > 0)
            {
                mensajes.Add($"{resultado.eliminados} pago(s) eliminado(s) exitosamente.");
            }
            if (resultado.noEncontrados > 0)
            {
                mensajes.Add($"{resultado.noEncontrados} pago(s) no encontrado(s).");
            }

            if (mensajes.Any())
            {
                if (resultado.eliminados > 0)
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
            TempData["Error"] = $"Error al eliminar pagos: {ex.Message}";
        }

        return Redirect("/pagos");
    }

    [Authorize(Policy = "Pagos")]
    [HttpGet("/pagos/exportar-excel")]
    public IActionResult ExportarExcel(string? tipoPago, string? banco, DateTime? fecha)
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Obtener pagos con filtros (igual que en Index)
            var query = _pagoService.ObtenerTodos().AsQueryable();

            // Aplicar filtros si existen
            if (!string.IsNullOrWhiteSpace(tipoPago) && tipoPago != "Todos")
            {
                query = query.Where(p => p.TipoPago == tipoPago);
            }

            if (!string.IsNullOrWhiteSpace(banco) && banco != "Todos")
            {
                query = query.Where(p => p.Banco == banco);
            }

            if (fecha.HasValue)
            {
                query = query.Where(p => p.FechaPago.Date == fecha.Value.Date);
            }

            var pagos = query
                .OrderByDescending(p => p.FechaPago)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Pagos");

                // Encabezados
                worksheet.Cells[1, 1].Value = "ID Pago";
                worksheet.Cells[1, 2].Value = "Fecha";
                worksheet.Cells[1, 3].Value = "Cliente";
                worksheet.Cells[1, 4].Value = "Factura(s)";
                worksheet.Cells[1, 5].Value = "Tipo Pago";
                worksheet.Cells[1, 6].Value = "Banco";
                worksheet.Cells[1, 7].Value = "Tipo Cuenta";
                worksheet.Cells[1, 8].Value = "Moneda";
                worksheet.Cells[1, 9].Value = "Monto";
                worksheet.Cells[1, 10].Value = "Córdobas Físico";
                worksheet.Cells[1, 11].Value = "Dólares Físico";
                worksheet.Cells[1, 12].Value = "Córdobas Electrónico";
                worksheet.Cells[1, 13].Value = "Dólares Electrónico";
                worksheet.Cells[1, 14].Value = "Monto Recibido";
                worksheet.Cells[1, 15].Value = "Vuelto";
                worksheet.Cells[1, 16].Value = "Tipo Cambio";
                worksheet.Cells[1, 17].Value = "Observaciones";

                // Formatear encabezados
                using (var range = worksheet.Cells[1, 1, 1, 17])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(16, 185, 129)); // Verde esmeralda
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }

                // Datos
                int row = 2;
                foreach (var pago in pagos)
                {
                    worksheet.Cells[row, 1].Value = pago.Id;
                    worksheet.Cells[row, 2].Value = pago.FechaPago.ToString("dd/MM/yyyy HH:mm");

                    // Cliente (obtener desde la factura principal o desde PagoFacturas)
                    string nombreCliente = "N/A";
                    string facturasNumeros = "";

                    if (pago.Factura != null)
                    {
                        nombreCliente = pago.Factura.Cliente?.Nombre ?? "N/A";
                        facturasNumeros = pago.Factura.Numero;
                    }
                    else if (pago.PagoFacturas != null && pago.PagoFacturas.Any())
                    {
                        var primeraFactura = pago.PagoFacturas.FirstOrDefault()?.Factura;
                        nombreCliente = primeraFactura?.Cliente?.Nombre ?? "N/A";
                        facturasNumeros = string.Join(", ", pago.PagoFacturas.Select(pf => pf.Factura?.Numero ?? "N/A"));
                    }

                    worksheet.Cells[row, 3].Value = nombreCliente;
                    worksheet.Cells[row, 4].Value = facturasNumeros;
                    worksheet.Cells[row, 5].Value = pago.TipoPago;
                    worksheet.Cells[row, 6].Value = pago.Banco ?? "-";
                    worksheet.Cells[row, 7].Value = pago.TipoCuenta ?? "-";
                    worksheet.Cells[row, 8].Value = pago.Moneda;
                    worksheet.Cells[row, 9].Value = pago.Monto;
                    worksheet.Cells[row, 10].Value = pago.MontoCordobasFisico ?? 0;
                    worksheet.Cells[row, 11].Value = pago.MontoDolaresFisico ?? 0;
                    worksheet.Cells[row, 12].Value = pago.MontoCordobasElectronico ?? 0;
                    worksheet.Cells[row, 13].Value = pago.MontoDolaresElectronico ?? 0;
                    worksheet.Cells[row, 14].Value = pago.MontoRecibidoFisico ?? 0;
                    worksheet.Cells[row, 15].Value = pago.VueltoFisico ?? 0;
                    worksheet.Cells[row, 16].Value = pago.TipoCambio ?? 0;
                    worksheet.Cells[row, 17].Value = pago.Observaciones ?? "-";

                    // Formatear montos como moneda
                    for (int col = 9; col <= 16; col++)
                    {
                        worksheet.Cells[row, col].Style.Numberformat.Format = "#,##0.00";
                    }

                    // Colorear según tipo de pago
                    var tipoCell = worksheet.Cells[row, 5];
                    switch (pago.TipoPago)
                    {
                        case "Fisico":
                            tipoCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            tipoCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(254, 249, 195)); // Amarillo
                            break;
                        case "Electronico":
                            tipoCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            tipoCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(219, 234, 254)); // Azul claro
                            break;
                        case "Mixto":
                            tipoCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            tipoCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(243, 232, 255)); // Morado claro
                            break;
                    }

                    row++;
                }

                // Ajustar ancho de columnas
                worksheet.Column(1).Width = 10;  // ID Pago
                worksheet.Column(2).Width = 18;  // Fecha
                worksheet.Column(3).Width = 30;  // Cliente
                worksheet.Column(4).Width = 20;  // Factura(s)
                worksheet.Column(5).Width = 15;  // Tipo Pago
                worksheet.Column(6).Width = 15;  // Banco
                worksheet.Column(7).Width = 18;  // Tipo Cuenta
                worksheet.Column(8).Width = 12;  // Moneda
                worksheet.Column(9).Width = 15;  // Monto
                worksheet.Column(10).Width = 16; // Córdobas Físico
                worksheet.Column(11).Width = 16; // Dólares Físico
                worksheet.Column(12).Width = 18; // Córdobas Electrónico
                worksheet.Column(13).Width = 18; // Dólares Electrónico
                worksheet.Column(14).Width = 16; // Monto Recibido
                worksheet.Column(15).Width = 12; // Vuelto
                worksheet.Column(16).Width = 13; // Tipo Cambio
                worksheet.Column(17).Width = 30; // Observaciones

                // Aplicar bordes a todas las celdas con datos
                if (row > 2)
                {
                    using (var range = worksheet.Cells[1, 1, row - 1, 17])
                    {
                        range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        range.Style.Border.Top.Color.SetColor(System.Drawing.Color.LightGray);
                        range.Style.Border.Bottom.Color.SetColor(System.Drawing.Color.LightGray);
                        range.Style.Border.Left.Color.SetColor(System.Drawing.Color.LightGray);
                        range.Style.Border.Right.Color.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Alinear contenido
                    using (var range = worksheet.Cells[2, 1, row - 1, 17])
                    {
                        range.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                    }

                    // Centrar ID
                    using (var range = worksheet.Cells[2, 1, row - 1, 1])
                    {
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    // Alinear números a la derecha
                    for (int col = 9; col <= 16; col++)
                    {
                        using (var range = worksheet.Cells[2, col, row - 1, col])
                        {
                            range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                        }
                    }

                    // Centrar Tipo Pago
                    using (var range = worksheet.Cells[2, 5, row - 1, 5])
                    {
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }
                }

                // Agregar totales al final
                if (row > 2)
                {
                    row++; // Fila vacía
                    worksheet.Cells[row, 8].Value = "TOTAL:";
                    worksheet.Cells[row, 8].Style.Font.Bold = true;
                    worksheet.Cells[row, 8].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    
                    worksheet.Cells[row, 9].Formula = $"SUM(I2:I{row - 2})";
                    worksheet.Cells[row, 9].Style.Font.Bold = true;
                    worksheet.Cells[row, 9].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[row, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));

                    // Resumen por tipo de pago
                    row += 2;
                    worksheet.Cells[row, 8].Value = "Resumen:";
                    worksheet.Cells[row, 8].Style.Font.Bold = true;
                    
                    row++;
                    worksheet.Cells[row, 8].Value = "Pagos Físicos:";
                    worksheet.Cells[row, 9].Value = pagos.Count(p => p.TipoPago == "Fisico");
                    
                    row++;
                    worksheet.Cells[row, 8].Value = "Pagos Electrónicos:";
                    worksheet.Cells[row, 9].Value = pagos.Count(p => p.TipoPago == "Electronico");
                    
                    row++;
                    worksheet.Cells[row, 8].Value = "Pagos Mixtos:";
                    worksheet.Cells[row, 9].Value = pagos.Count(p => p.TipoPago == "Mixto");
                }

                // Congelar primera fila
                worksheet.View.FreezePanes(2, 1);

                // Generar nombre del archivo con fecha y filtros aplicados
                var nombreArchivo = "Pagos";
                if (fecha.HasValue)
                {
                    nombreArchivo += $"_{fecha.Value:yyyyMMdd}";
                }
                if (!string.IsNullOrWhiteSpace(tipoPago) && tipoPago != "Todos")
                {
                    nombreArchivo += $"_{tipoPago}";
                }
                if (!string.IsNullOrWhiteSpace(banco) && banco != "Todos")
                {
                    nombreArchivo += $"_{banco}";
                }
                nombreArchivo += $"_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(package.GetAsByteArray(), contentType, nombreArchivo);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al exportar pagos: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace billing_system.Controllers;

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
        var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
        ViewBag.TipoCambio = tipoCambio;
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
                // Obtener tipo de cambio para convertir dólares a córdobas
                var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
                
                // Calcular el total recibido en córdobas: córdobas + (dólares * tipo de cambio)
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
            
            var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
            ViewBag.TipoCambio = tipoCambio;
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
            var tipoCambio = _configuracionService.ObtenerValorDecimal("TipoCambioDolar") ?? SD.TipoCambioDolar;
            ViewBag.TipoCambio = tipoCambio;
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
}

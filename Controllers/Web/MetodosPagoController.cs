using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class MetodosPagoController : Controller
{
    private readonly IMetodoPagoService _metodoPagoService;

    public MetodosPagoController(IMetodoPagoService metodoPagoService)
    {
        _metodoPagoService = metodoPagoService;
    }

    [HttpGet("/metodos-pago")]
    public IActionResult Index()
    {
        var metodosPago = _metodoPagoService.ObtenerTodos();
        ViewBag.EsAdministrador = SecurityHelper.IsAdministrator(User);
        return View(metodosPago);
    }

    [HttpGet("/metodos-pago/crear")]
    public IActionResult Crear()
    {
        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
        ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
        ViewBag.Monedas = new[] { "C$", "$", "📱" };
        ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
        return View();
    }

    [HttpPost("/metodos-pago/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(MetodoPago metodoPago)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(metodoPago.NombreBanco))
        {
            ModelState.AddModelError("NombreBanco", "El nombre del banco es requerido.");
        }

        if (string.IsNullOrWhiteSpace(metodoPago.TipoCuenta))
        {
            ModelState.AddModelError("TipoCuenta", "El tipo de cuenta es requerido.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
            ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
            ViewBag.Monedas = new[] { "C$", "$", "📱" };
            ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
            return View(metodoPago);
        }

        try
        {
            var metodoPagoCreado = _metodoPagoService.Crear(metodoPago);
            TempData["Success"] = $"✅ Método de pago '{metodoPagoCreado.NombreBanco} - {metodoPagoCreado.TipoCuenta}' creado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear método de pago: {ex.Message}";
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
            ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
            ViewBag.Monedas = new[] { "C$", "$", "📱" };
            ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
            return View(metodoPago);
        }
    }

    [HttpGet("/metodos-pago/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var metodoPago = _metodoPagoService.ObtenerPorId(id);
        if (metodoPago == null)
        {
            TempData["Error"] = "Método de pago no encontrado";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
        ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
        ViewBag.Monedas = new[] { "C$", "$", "📱" };
        ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
        return View(metodoPago);
    }

    [HttpPost("/metodos-pago/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, MetodoPago metodoPago)
    {
        if (id != metodoPago.Id)
        {
            TempData["Error"] = "ID de método de pago no coincide";
            return RedirectToAction(nameof(Index));
        }

        // Validaciones
        if (string.IsNullOrWhiteSpace(metodoPago.NombreBanco))
        {
            ModelState.AddModelError("NombreBanco", "El nombre del banco es requerido.");
        }

        if (string.IsNullOrWhiteSpace(metodoPago.TipoCuenta))
        {
            ModelState.AddModelError("TipoCuenta", "El tipo de cuenta es requerido.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
            ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
            ViewBag.Monedas = new[] { "C$", "$", "📱" };
            ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
            return View(metodoPago);
        }

        try
        {
            if (_metodoPagoService.Actualizar(metodoPago))
            {
                TempData["Success"] = $"✅ Método de pago '{metodoPago.NombreBanco} - {metodoPago.TipoCuenta}' actualizado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el método de pago";
            }
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al actualizar método de pago: {ex.Message}";
            ViewBag.Bancos = new[] { SD.BancoBanpro, SD.BancoLafise, SD.BancoBAC, SD.BancoFicohsa, SD.BancoBDF, "Otro" };
            ViewBag.TiposCuenta = new[] { "Córdobas", "Dólares", "Billetera Móvil", "Próximamente" };
            ViewBag.Monedas = new[] { "C$", "$", "📱" };
            ViewBag.Iconos = new[] { "🏦", "🏛️", "💳", "📱" };
            return View(metodoPago);
        }
    }

    [HttpPost("/metodos-pago/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var metodoPago = _metodoPagoService.ObtenerPorId(id);
            if (metodoPago == null)
            {
                TempData["Error"] = "Método de pago no encontrado";
                return RedirectToAction(nameof(Index));
            }

            if (_metodoPagoService.Eliminar(id))
            {
                TempData["Success"] = $"✅ Método de pago '{metodoPago.NombreBanco} - {metodoPago.TipoCuenta}' eliminado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el método de pago";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar método de pago: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/metodos-pago/toggle-activo/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivo(int id)
    {
        try
        {
            var metodoPago = _metodoPagoService.ObtenerPorId(id);
            if (metodoPago == null)
            {
                TempData["Error"] = "Método de pago no encontrado";
                return RedirectToAction(nameof(Index));
            }

            metodoPago.Activo = !metodoPago.Activo;
            if (_metodoPagoService.Actualizar(metodoPago))
            {
                var estado = metodoPago.Activo ? "activado" : "desactivado";
                TempData["Success"] = $"✅ Método de pago '{metodoPago.NombreBanco}' {estado} exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo cambiar el estado";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/metodos-pago/actualizar-orden")]
    [ValidateAntiForgeryToken]
    public IActionResult ActualizarOrden([FromBody] Dictionary<int, int> ordenPorId)
    {
        try
        {
            if (_metodoPagoService.ActualizarOrden(ordenPorId))
            {
                return Json(new { success = true, message = "Orden actualizado exitosamente" });
            }
            return Json(new { success = false, message = "No se pudo actualizar el orden" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }
}


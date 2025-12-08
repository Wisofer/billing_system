using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;

namespace billing_system.Controllers.Api;

/// <summary>
/// API pública para la landing page (React)
/// No requiere autenticación ya que es información pública
/// </summary>
[ApiController]
[Route("api/landing")]
public class LandingPageController : ControllerBase
{
    private readonly IServicioLandingPageService _servicioLandingPageService;
    private readonly IMetodoPagoService _metodoPagoService;

    public LandingPageController(IServicioLandingPageService servicioLandingPageService, IMetodoPagoService metodoPagoService)
    {
        _servicioLandingPageService = servicioLandingPageService;
        _metodoPagoService = metodoPagoService;
    }

    /// <summary>
    /// Obtiene todos los servicios de internet activos para la landing page
    /// GET /api/landing/servicios
    /// </summary>
    [HttpGet("servicios")]
    public IActionResult ObtenerServicios()
    {
        try
        {
            var servicios = _servicioLandingPageService.ObtenerActivosOrdenados()
                .Select(s => new
                {
                    id = s.Id,
                    titulo = s.Titulo,
                    descripcion = s.Descripcion,
                    precio = s.Precio,
                    velocidad = s.Velocidad,
                    etiqueta = s.Etiqueta,
                    colorEtiqueta = s.ColorEtiqueta,
                    icono = s.Icono,
                    caracteristicas = s.Caracteristicas,
                    orden = s.Orden,
                    destacado = s.Destacado,
                    activo = s.Activo
                })
                .ToList();

            return Ok(new { success = true, data = servicios });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al obtener servicios: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtiene todos los métodos de pago activos para la landing page
    /// GET /api/landing/metodos-pago
    /// </summary>
    [HttpGet("metodos-pago")]
    public IActionResult ObtenerMetodosPago()
    {
        try
        {
            var metodosPago = _metodoPagoService.ObtenerActivosOrdenados()
                .Select(m => new
                {
                    id = m.Id,
                    nombreBanco = m.NombreBanco,
                    icono = m.Icono,
                    tipoCuenta = m.TipoCuenta,
                    moneda = m.Moneda,
                    numeroCuenta = m.NumeroCuenta,
                    mensaje = m.Mensaje,
                    orden = m.Orden,
                    activo = m.Activo
                })
                .ToList();

            return Ok(new { success = true, data = metodosPago });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al obtener métodos de pago: {ex.Message}" });
        }
    }

    /// <summary>
    /// Obtiene información general para la landing page
    /// GET /api/landing/info
    /// </summary>
    [HttpGet("info")]
    public IActionResult ObtenerInfo()
    {
        try
        {
            var servicios = _servicioLandingPageService.ObtenerActivosOrdenados()
                .Select(s => new
                {
                    id = s.Id,
                    titulo = s.Titulo,
                    descripcion = s.Descripcion,
                    precio = s.Precio,
                    velocidad = s.Velocidad,
                    etiqueta = s.Etiqueta,
                    colorEtiqueta = s.ColorEtiqueta,
                    icono = s.Icono,
                    caracteristicas = s.Caracteristicas,
                    orden = s.Orden,
                    destacado = s.Destacado,
                    activo = s.Activo
                })
                .ToList();

            var metodosPago = _metodoPagoService.ObtenerActivosOrdenados()
                .Select(m => new
                {
                    id = m.Id,
                    nombreBanco = m.NombreBanco,
                    icono = m.Icono,
                    tipoCuenta = m.TipoCuenta,
                    moneda = m.Moneda,
                    numeroCuenta = m.NumeroCuenta,
                    mensaje = m.Mensaje,
                    orden = m.Orden,
                    activo = m.Activo
                })
                .ToList();

            return Ok(new 
            { 
                success = true, 
                data = new 
                { 
                    servicios = servicios,
                    metodosPago = metodosPago
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al obtener información: {ex.Message}" });
        }
    }
}


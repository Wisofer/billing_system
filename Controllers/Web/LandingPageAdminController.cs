using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class LandingPageAdminController : Controller
{
    private readonly IServicioLandingPageService _servicioLandingPageService;
    private readonly IMetodoPagoService _metodoPagoService;

    public LandingPageAdminController(IServicioLandingPageService servicioLandingPageService, IMetodoPagoService metodoPagoService)
    {
        _servicioLandingPageService = servicioLandingPageService;
        _metodoPagoService = metodoPagoService;
    }

    [HttpGet("/landing-admin")]
    public IActionResult Index()
    {
        // Obtener estadísticas
        var servicios = _servicioLandingPageService.ObtenerTodos();
        var metodosPago = _metodoPagoService.ObtenerTodos();

        ViewBag.TotalServicios = servicios.Count;
        ViewBag.ServiciosActivos = servicios.Count(s => s.Activo);
        ViewBag.ServiciosDestacados = servicios.Count(s => s.Destacado);
        
        ViewBag.TotalMetodosPago = metodosPago.Count;
        ViewBag.MetodosPagoActivos = metodosPago.Count(m => m.Activo);

        return View();
    }
}


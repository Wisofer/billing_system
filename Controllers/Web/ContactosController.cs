using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;

namespace billing_system.Controllers.Web
{
    [Authorize(Policy = "Administrador")]
    public class ContactosController : Controller
    {
        private readonly IContactoService _contactoService;

        public ContactosController(IContactoService contactoService)
        {
            _contactoService = contactoService;
        }

        public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = 15, string? estado = null, string? busqueda = null)
        {
            var resultado = await _contactoService.ObtenerPaginados(pagina, tamanoPagina, estado, busqueda);
            
            // Estadísticas
            ViewBag.TotalNuevos = await _contactoService.ContarPorEstado("Nuevo");
            ViewBag.TotalLeidos = await _contactoService.ContarPorEstado("Leído");
            ViewBag.TotalRespondidos = await _contactoService.ContarPorEstado("Respondido");
            
            // Filtros actuales
            ViewBag.Estado = estado;
            ViewBag.Busqueda = busqueda;
            ViewBag.TamanoPagina = tamanoPagina;

            return View(resultado);
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var contacto = await _contactoService.ObtenerPorId(id);
            if (contacto == null)
            {
                TempData["Error"] = "El mensaje de contacto no existe.";
                return RedirectToAction("Index");
            }

            // Marcar como leído automáticamente al ver el detalle
            if (contacto.Estado == "Nuevo")
            {
                await _contactoService.MarcarComoLeido(id);
                contacto.Estado = "Leído";
                contacto.FechaLeido = DateTime.Now;
            }

            return View(contacto);
        }

        [HttpPost]
        public async Task<IActionResult> MarcarLeido(int id)
        {
            var contacto = await _contactoService.MarcarComoLeido(id);
            if (contacto == null)
            {
                TempData["Error"] = "El mensaje de contacto no existe.";
            }
            else
            {
                TempData["Success"] = "Mensaje marcado como leído.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MarcarRespondido(int id)
        {
            var contacto = await _contactoService.MarcarComoRespondido(id);
            if (contacto == null)
            {
                TempData["Error"] = "El mensaje de contacto no existe.";
            }
            else
            {
                TempData["Success"] = "Mensaje marcado como respondido.";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _contactoService.Eliminar(id);
            if (resultado)
            {
                TempData["Success"] = "Mensaje eliminado correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el mensaje.";
            }
            return RedirectToAction("Index");
        }
    }
}


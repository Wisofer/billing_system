using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class CategoriasEquipoController : Controller
{
    private readonly ICategoriaEquipoService _categoriaEquipoService;
    private readonly IEquipoService _equipoService;

    public CategoriasEquipoController(
        ICategoriaEquipoService categoriaEquipoService,
        IEquipoService equipoService)
    {
        _categoriaEquipoService = categoriaEquipoService;
        _equipoService = equipoService;
    }

    [HttpGet("/categorias-equipo")]
    public IActionResult Index(string? busqueda)
    {
        var categorias = _categoriaEquipoService.ObtenerTodas();
        
        // Filtrar por búsqueda si existe
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.ToLower();
            categorias = categorias.Where(c => 
                c.Nombre.ToLower().Contains(termino) ||
                (c.Descripcion != null && c.Descripcion.ToLower().Contains(termino))
            ).ToList();
        }

        // Obtener cantidad de equipos por categoría
        var todosEquipos = _equipoService.ObtenerTodos();
        var categoriasConConteo = categorias.Select(c => new
        {
            Categoria = c,
            CantidadEquipos = todosEquipos.Count(e => e.CategoriaEquipoId == c.Id && e.Activo)
        }).OrderBy(x => x.Categoria.Nombre).ToList();

        ViewBag.Busqueda = busqueda;
        ViewBag.CategoriasConConteo = categoriasConConteo;
        
        return View(categorias);
    }

    [HttpGet("/categorias-equipo/crear")]
    public IActionResult Crear()
    {
        return View(new CategoriaEquipo());
    }

    [HttpPost("/categorias-equipo/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(CategoriaEquipo categoria)
    {
        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        try
        {
            var categoriaCreada = _categoriaEquipoService.Crear(categoria);
            TempData["Success"] = $"✅ Categoría '{categoriaCreada.Nombre}' creada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(categoria);
        }
    }

    [HttpGet("/categorias-equipo/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var categoria = _categoriaEquipoService.ObtenerPorId(id);
        if (categoria == null)
        {
            TempData["Error"] = "Categoría no encontrada";
            return RedirectToAction(nameof(Index));
        }

        return View(categoria);
    }

    [HttpPost("/categorias-equipo/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, CategoriaEquipo categoria)
    {
        if (id != categoria.Id)
        {
            TempData["Error"] = "ID de categoría no coincide";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        try
        {
            var categoriaActualizada = _categoriaEquipoService.Actualizar(categoria);
            TempData["Success"] = $"✅ Categoría '{categoriaActualizada.Nombre}' actualizada exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(categoria);
        }
    }

    [HttpPost("/categorias-equipo/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var categoria = _categoriaEquipoService.ObtenerPorId(id);
            if (categoria == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToAction(nameof(Index));
            }

            // Verificar si tiene equipos asociados
            var cantidadEquipos = _equipoService.ObtenerTodos().Count(e => e.CategoriaEquipoId == id && e.Activo);
            if (cantidadEquipos > 0)
            {
                TempData["Error"] = $"No se puede eliminar la categoría '{categoria.Nombre}' porque tiene {cantidadEquipos} equipo(s) asociado(s). Desactívala en lugar de eliminarla.";
                return RedirectToAction(nameof(Index));
            }

            var eliminada = _categoriaEquipoService.Eliminar(id);
            if (eliminada)
            {
                TempData["Success"] = $"✅ Categoría '{categoria.Nombre}' eliminada exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar la categoría";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/categorias-equipo/toggle-activo/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivo(int id)
    {
        try
        {
            var categoria = _categoriaEquipoService.ObtenerPorId(id);
            if (categoria == null)
            {
                TempData["Error"] = "Categoría no encontrada";
                return RedirectToAction(nameof(Index));
            }

            categoria.Activo = !categoria.Activo;
            _categoriaEquipoService.Actualizar(categoria);

            var estado = categoria.Activo ? "activada" : "desactivada";
            TempData["Success"] = $"✅ Categoría '{categoria.Nombre}' {estado} exitosamente";
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}


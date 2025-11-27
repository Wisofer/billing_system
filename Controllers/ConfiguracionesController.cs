using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using billing_system.Data;
using System.Security.Cryptography;
using System.Text;

namespace billing_system.Controllers;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class ConfiguracionesController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly ApplicationDbContext _context;

    public ConfiguracionesController(IUsuarioService usuarioService, ApplicationDbContext context)
    {
        _usuarioService = usuarioService;
        _context = context;
    }

    [HttpGet("/configuraciones")]
    public IActionResult Index()
    {
        // Obtener información del usuario desde Claims
        var nombreUsuario = User.Identity?.Name ?? "";
        var rolUsuario = User.FindFirst("Rol")?.Value ?? "";
        var esAdministrador = User.HasClaim("Rol", "Administrador");
        var temaActual = HttpContext.Session.GetString("Tema") ?? "claro";

        var usuarios = _usuarioService.ObtenerTodos();
        var plantillas = _context.PlantillasMensajeWhatsApp.OrderByDescending(p => p.EsDefault).ThenBy(p => p.Nombre).ToList();

        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.RolUsuario = rolUsuario;
        ViewBag.NombreUsuario = nombreUsuario;
        ViewBag.Usuarios = usuarios;
        ViewBag.TemaActual = temaActual;
        ViewBag.Plantillas = plantillas;

        return View();
    }

    [HttpPost("/configuraciones/usuarios/crear")]
    public IActionResult CrearUsuario([FromForm] string nombreUsuario, [FromForm] string contrasena, 
        [FromForm] string nombreCompleto, [FromForm] string rol, [FromForm] bool activo)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario) || string.IsNullOrWhiteSpace(contrasena))
        {
            return Json(new { success = false, message = "El nombre de usuario y la contraseña son requeridos." });
        }

        if (_usuarioService.ExisteNombreUsuario(nombreUsuario))
        {
            return Json(new { success = false, message = "El nombre de usuario ya existe." });
        }

        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario.Trim(),
            Contrasena = HashPassword(contrasena),
            NombreCompleto = nombreCompleto?.Trim() ?? nombreUsuario.Trim(),
            Rol = rol == "Administrador" ? "Administrador" : "Normal",
            Activo = activo
        };

        if (_usuarioService.Crear(usuario))
        {
            return Json(new { success = true, message = "Usuario creado exitosamente." });
        }

        return Json(new { success = false, message = "Error al crear el usuario." });
    }

    [HttpPost("/configuraciones/usuarios/editar")]
    public IActionResult EditarUsuario([FromForm] int id, [FromForm] string nombreUsuario, 
        [FromForm] string? contrasena, [FromForm] string nombreCompleto, 
        [FromForm] string rol, [FromForm] bool activo)
    {
        var usuario = _usuarioService.ObtenerPorId(id);
        if (usuario == null)
        {
            return Json(new { success = false, message = "Usuario no encontrado." });
        }

        if (_usuarioService.ExisteNombreUsuario(nombreUsuario, id))
        {
            return Json(new { success = false, message = "El nombre de usuario ya existe." });
        }

        usuario.NombreUsuario = nombreUsuario.Trim();
        usuario.NombreCompleto = nombreCompleto?.Trim() ?? nombreUsuario.Trim();
        usuario.Rol = rol == "Administrador" ? "Administrador" : "Normal";
        usuario.Activo = activo;

        // Solo actualizar contraseña si se proporcionó una nueva
        if (!string.IsNullOrWhiteSpace(contrasena))
        {
            usuario.Contrasena = HashPassword(contrasena);
        }

        if (_usuarioService.Actualizar(usuario))
        {
            return Json(new { success = true, message = "Usuario actualizado exitosamente." });
        }

        return Json(new { success = false, message = "Error al actualizar el usuario." });
    }

    [HttpPost("/configuraciones/usuarios/eliminar")]
    public IActionResult EliminarUsuario([FromForm] int id)
    {
        var usuarioActualId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (usuarioActualId != null && int.TryParse(usuarioActualId, out var idActual) && idActual == id)
        {
            return Json(new { success = false, message = "No puedes eliminar tu propio usuario." });
        }

        if (_usuarioService.Eliminar(id))
        {
            return Json(new { success = true, message = "Usuario eliminado exitosamente." });
        }

        return Json(new { success = false, message = "Error al eliminar el usuario." });
    }

    [HttpPost("/configuraciones/tema")]
    public IActionResult CambiarTema([FromForm] string tema)
    {
        HttpContext.Session.SetString("Tema", tema);
        return Json(new { success = true, tema = tema });
    }

    // ========== CRUD de Plantillas WhatsApp ==========
    
    [HttpPost("/configuraciones/plantillas/crear")]
    public IActionResult CrearPlantilla([FromForm] string nombre, [FromForm] string mensaje, [FromForm] bool activa, [FromForm] bool esDefault)
    {
        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(mensaje))
        {
            return Json(new { success = false, message = "El nombre y el mensaje son requeridos." });
        }

        // Si se marca como default, quitar el default de las demás
        if (esDefault)
        {
            var otrasDefault = _context.PlantillasMensajeWhatsApp.Where(p => p.EsDefault).ToList();
            foreach (var p in otrasDefault)
            {
                p.EsDefault = false;
            }
        }

        var plantilla = new PlantillaMensajeWhatsApp
        {
            Nombre = nombre.Trim(),
            Mensaje = mensaje.Trim(),
            Activa = activa,
            EsDefault = esDefault,
            FechaCreacion = DateTime.Now
        };

        _context.PlantillasMensajeWhatsApp.Add(plantilla);
        _context.SaveChanges();

        return Json(new { success = true, message = "Plantilla creada exitosamente." });
    }

    [HttpPost("/configuraciones/plantillas/editar")]
    public IActionResult EditarPlantilla([FromForm] int id, [FromForm] string nombre, [FromForm] string mensaje, [FromForm] bool activa, [FromForm] bool esDefault)
    {
        var plantilla = _context.PlantillasMensajeWhatsApp.Find(id);
        if (plantilla == null)
        {
            return Json(new { success = false, message = "Plantilla no encontrada." });
        }

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(mensaje))
        {
            return Json(new { success = false, message = "El nombre y el mensaje son requeridos." });
        }

        // Si se marca como default, quitar el default de las demás
        if (esDefault && !plantilla.EsDefault)
        {
            var otrasDefault = _context.PlantillasMensajeWhatsApp.Where(p => p.EsDefault && p.Id != id).ToList();
            foreach (var p in otrasDefault)
            {
                p.EsDefault = false;
            }
        }

        plantilla.Nombre = nombre.Trim();
        plantilla.Mensaje = mensaje.Trim();
        plantilla.Activa = activa;
        plantilla.EsDefault = esDefault;
        plantilla.FechaActualizacion = DateTime.Now;

        _context.SaveChanges();

        return Json(new { success = true, message = "Plantilla actualizada exitosamente." });
    }

    [HttpPost("/configuraciones/plantillas/eliminar")]
    public IActionResult EliminarPlantilla([FromForm] int id)
    {
        var plantilla = _context.PlantillasMensajeWhatsApp.Find(id);
        if (plantilla == null)
        {
            return Json(new { success = false, message = "Plantilla no encontrada." });
        }

        // No permitir eliminar si es la única plantilla default
        if (plantilla.EsDefault)
        {
            var otrasDefault = _context.PlantillasMensajeWhatsApp.Any(p => p.EsDefault && p.Id != id);
            if (!otrasDefault)
            {
                return Json(new { success = false, message = "No se puede eliminar la única plantilla por defecto. Crea otra plantilla por defecto primero." });
            }
        }

        _context.PlantillasMensajeWhatsApp.Remove(plantilla);
        _context.SaveChanges();

        return Json(new { success = true, message = "Plantilla eliminada exitosamente." });
    }

    [HttpPost("/configuraciones/plantillas/marcar-default")]
    public IActionResult MarcarPlantillaDefault([FromForm] int id)
    {
        var plantilla = _context.PlantillasMensajeWhatsApp.Find(id);
        if (plantilla == null)
        {
            return Json(new { success = false, message = "Plantilla no encontrada." });
        }

        // Quitar default de todas las demás
        var otrasDefault = _context.PlantillasMensajeWhatsApp.Where(p => p.EsDefault && p.Id != id).ToList();
        foreach (var p in otrasDefault)
        {
            p.EsDefault = false;
        }

        plantilla.EsDefault = true;
        plantilla.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();

        return Json(new { success = true, message = "Plantilla marcada como predeterminada." });
    }

    private string HashPassword(string password)
    {
        return PasswordHelper.HashPassword(password);
    }
}

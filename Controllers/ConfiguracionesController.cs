using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Security.Cryptography;
using System.Text;

namespace billing_system.Controllers;

[Route("[controller]/[action]")]
public class ConfiguracionesController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public ConfiguracionesController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet("/configuraciones")]
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UsuarioActual") == null)
        {
            return Redirect("/login");
        }

        var esAdministrador = Helpers.EsAdministrador(HttpContext.Session);
        if (!esAdministrador)
        {
            TempData["Error"] = "Solo los administradores pueden acceder a esta sección.";
            return Redirect("/");
        }

        var usuarios = _usuarioService.ObtenerTodos();
        var rolUsuario = HttpContext.Session.GetString("RolUsuario") ?? "";
        var nombreUsuario = HttpContext.Session.GetString("NombreUsuario") ?? "";

        ViewBag.EsAdministrador = esAdministrador;
        ViewBag.RolUsuario = rolUsuario;
        ViewBag.NombreUsuario = nombreUsuario;
        ViewBag.Usuarios = usuarios;

        return View();
    }

    [HttpPost("/configuraciones/usuarios/crear")]
    public IActionResult CrearUsuario([FromForm] string nombreUsuario, [FromForm] string contrasena, 
        [FromForm] string nombreCompleto, [FromForm] string rol, [FromForm] bool activo)
    {
        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            return Json(new { success = false, message = "No tienes permisos para realizar esta acción." });
        }

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
        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            return Json(new { success = false, message = "No tienes permisos para realizar esta acción." });
        }

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
        if (!Helpers.EsAdministrador(HttpContext.Session))
        {
            return Json(new { success = false, message = "No tienes permisos para realizar esta acción." });
        }

        var usuarioActual = Helpers.ObtenerUsuarioActual(HttpContext.Session);
        if (usuarioActual != null && usuarioActual.Id == id)
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

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

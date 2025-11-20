using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using System.Text.Json;

namespace billing_system.Controllers;

[Route("[controller]/[action]")]
public class AuthController : Controller
{
    [HttpGet("/login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("/login")]
    public IActionResult Login(string NombreUsuario, string Contrasena)
    {
        // Crear usuario automáticamente con cualquier dato ingresado
        var usuario = new Usuario
        {
            Id = 1,
            NombreUsuario = NombreUsuario ?? "invitado",
            Contrasena = Contrasena ?? "",
            Rol = (NombreUsuario?.ToLower() == "admin") ? "Administrador" : "Normal",
            NombreCompleto = NombreUsuario ?? "invitado",
            Activo = true
        };

        // Guardar en sesión
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        HttpContext.Session.SetString("RolUsuario", usuario.Rol);
        HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);

        // Redirigir al home
        return Redirect("/");
    }

    [HttpPost("/logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Redirect("/login");
    }
}


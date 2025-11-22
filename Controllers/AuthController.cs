using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using System.Security.Claims;
using System.Text.Json;

namespace billing_system.Controllers;

[Route("[controller]/[action]")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("/login")]
    public IActionResult Login()
    {
        // Si ya está autenticado, redirigir al home
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/");
        }
        return View();
    }

    [HttpPost("/login")]
    public async Task<IActionResult> Login(string NombreUsuario, string Contrasena)
    {
        if (string.IsNullOrWhiteSpace(NombreUsuario) || string.IsNullOrWhiteSpace(Contrasena))
        {
            ViewBag.Error = "El nombre de usuario y la contraseña son requeridos.";
            return View();
        }

        // Validar usuario contra la base de datos
        var usuario = _authService.ValidarUsuario(NombreUsuario, Contrasena);

        if (usuario == null)
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        // Crear claims (información del usuario)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.NombreUsuario),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim("Rol", usuario.Rol),
            new Claim("NombreCompleto", usuario.NombreCompleto)
        };

        // Crear identidad y principal
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        // Guardar también en sesión para compatibilidad con código existente
        HttpContext.Session.SetString("UsuarioActual", JsonSerializer.Serialize(usuario));
        HttpContext.Session.SetString("RolUsuario", usuario.Rol);
        HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);

        // Iniciar sesión (crea la cookie de autenticación)
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
        {
            IsPersistent = false, // No recordar sesión después de cerrar navegador
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // 30 minutos
        });

        // Redirigir al home
        return Redirect("/");
    }

    [HttpPost("/logout")]
    public async Task<IActionResult> Logout()
    {
        // Cerrar sesión de autenticación
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        // Limpiar sesión
        HttpContext.Session.Clear();
        
        return Redirect("/login");
    }
}

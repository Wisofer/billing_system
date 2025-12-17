using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using System.Security.Claims;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Iniciar sesión - POST /api/movil/auth/login
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Contrasena))
                {
                    return BadRequest(new { success = false, message = "Usuario y contraseña son requeridos" });
                }

                var usuario = _authService.ValidarUsuario(request.Usuario, request.Contrasena);
                
                if (usuario == null)
                {
                    return Unauthorized(new { success = false, message = "Credenciales inválidas" });
                }

                // Generar token simple (en producción usar JWT)
                var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

                return Ok(new
                {
                    success = true,
                    message = "Inicio de sesión exitoso",
                    data = new
                    {
                        token = token,
                        usuario = new
                        {
                            id = usuario.Id,
                            nombreUsuario = usuario.NombreUsuario,
                            nombreCompleto = usuario.NombreCompleto,
                            rol = usuario.Rol,
                            esAdmin = _authService.EsAdministrador(usuario)
                        }
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
            }
        }

        /// <summary>
        /// Obtener información del usuario actual - GET /api/movil/auth/me
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRole = User.FindFirst("Rol")?.Value;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = userId,
                        nombreUsuario = userName,
                        rol = userRole
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener información del usuario" });
            }
        }
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }
}

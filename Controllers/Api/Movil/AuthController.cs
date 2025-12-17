using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(IAuthService authService, JwtSettings jwtSettings)
        {
            _authService = authService;
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Iniciar sesión - POST /api/movil/auth/login
        /// Retorna un token JWT válido para usar en las demás APIs
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

                // Generar token JWT real
                var token = GenerarTokenJwt(usuario);
                var expiracion = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

                return Ok(new
                {
                    success = true,
                    message = "Inicio de sesión exitoso",
                    data = new
                    {
                        token = token,
                        tokenType = "Bearer",
                        expiresIn = _jwtSettings.ExpirationHours * 3600, // segundos
                        expiresAt = expiracion,
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
        /// Requiere token JWT válido en el header: Authorization: Bearer {token}
        /// </summary>
        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userName = User.FindFirst(ClaimTypes.Name)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var nombreCompleto = User.FindFirst("NombreCompleto")?.Value;

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = userId,
                        nombreUsuario = userName,
                        nombreCompleto = nombreCompleto,
                        rol = userRole
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener información del usuario" });
            }
        }

        /// <summary>
        /// Verificar si el token es válido - GET /api/movil/auth/verificar
        /// </summary>
        [HttpGet("verificar")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public IActionResult VerificarToken()
        {
            return Ok(new
            {
                success = true,
                message = "Token válido",
                data = new
                {
                    valido = true,
                    expira = User.FindFirst("exp")?.Value
                }
            });
        }

        /// <summary>
        /// Genera un token JWT con los claims del usuario
        /// </summary>
        private string GenerarTokenJwt(Models.Entities.Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("NombreCompleto", usuario.NombreCompleto ?? usuario.NombreUsuario),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }
}

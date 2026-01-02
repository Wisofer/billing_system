using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Utils;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace billing_system.Controllers.Api.Cliente
{
    [Route("api/cliente/auth")]
    [ApiController]
    public class ClienteAuthController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        private readonly JwtSettings _jwtSettings;

        public ClienteAuthController(IClienteService clienteService, JwtSettings jwtSettings)
        {
            _clienteService = clienteService;
            _jwtSettings = jwtSettings;
        }

        /// <summary>
        /// Iniciar sesión con código de cliente - POST /api/cliente/auth/login
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] ClienteLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Codigo))
                {
                    return BadRequest(new { success = false, message = "El código de cliente es requerido" });
                }

                var cliente = _clienteService.ObtenerPorCodigo(request.Codigo.Trim());
                
                if (cliente == null)
                {
                    return Unauthorized(new { success = false, message = "Código de cliente inválido" });
                }

                if (!cliente.Activo)
                {
                    return Unauthorized(new { success = false, message = "Cliente inactivo. Contacte a soporte." });
                }

                // Generar token JWT
                var token = GenerarTokenJwt(cliente);
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
                        cliente = new
                        {
                            id = cliente.Id,
                            codigo = cliente.Codigo,
                            nombre = cliente.Nombre,
                            activo = cliente.Activo
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
        /// Verificar si el token es válido - GET /api/cliente/auth/verificar
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
                    valido = true
                }
            });
        }

        /// <summary>
        /// Genera un token JWT con los claims del cliente
        /// </summary>
        private string GenerarTokenJwt(Models.Entities.Cliente cliente)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, cliente.Id.ToString()),
                new Claim("ClienteId", cliente.Id.ToString()),
                new Claim(ClaimTypes.Name, cliente.Codigo),
                new Claim("Codigo", cliente.Codigo),
                new Claim("Nombre", cliente.Nombre ?? cliente.Codigo),
                new Claim(ClaimTypes.Role, "Cliente"), // Rol fijo para clientes
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

    public class ClienteLoginRequest
    {
        public string Codigo { get; set; } = string.Empty;
    }
}

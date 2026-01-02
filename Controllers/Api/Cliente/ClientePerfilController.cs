using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using System.Security.Claims;

namespace billing_system.Controllers.Api.Cliente
{
    [Route("api/cliente/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class PerfilController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public PerfilController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        /// <summary>
        /// Obtener perfil del cliente - GET /api/cliente/perfil
        /// </summary>
        [HttpGet]
        public IActionResult GetPerfil()
        {
            try
            {
                var clienteId = ObtenerClienteId();
                if (!clienteId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Cliente no autenticado" });
                }

                var cliente = _clienteService.ObtenerPorId(clienteId.Value);

                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                var serviciosActivos = _clienteService.ObtenerServiciosActivos(clienteId.Value);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = cliente.Id,
                        codigo = cliente.Codigo,
                        nombre = cliente.Nombre,
                        telefono = cliente.Telefono,
                        email = cliente.Email,
                        cedula = cliente.Cedula,
                        activo = cliente.Activo,
                        fechaCreacion = cliente.FechaCreacion,
                        servicios = serviciosActivos.Select(cs => new
                        {
                            id = cs.Servicio?.Id,
                            nombre = cs.Servicio?.Nombre,
                            descripcion = cs.Servicio?.Descripcion,
                            categoria = cs.Servicio?.Categoria,
                            fechaInicio = cs.FechaInicio
                        })
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener perfil" });
            }
        }

        private int? ObtenerClienteId()
        {
            var clienteIdClaim = User.FindFirst("ClienteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(clienteIdClaim, out var clienteId))
            {
                return clienteId;
            }
            return null;
        }
    }
}



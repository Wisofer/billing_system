using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;
using billing_system.Models.Entities;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        /// <summary>
        /// Obtener todos los clientes paginados - GET /api/movil/clientes
        /// </summary>
        [HttpGet]
        public IActionResult GetAll(
            [FromQuery] int pagina = 1, 
            [FromQuery] int tamanoPagina = 20,
            [FromQuery] string? busqueda = null,
            [FromQuery] string? estado = null,
            [FromQuery] string? tipoServicio = null)
        {
            try
            {
                var resultado = _clienteService.ObtenerPaginados(pagina, tamanoPagina, busqueda, estado, tipoServicio);

                return Ok(new
                {
                    success = true,
                    data = resultado.Items.Select(c => new
                    {
                        id = c.Id,
                        codigo = c.Codigo,
                        nombre = c.Nombre,
                        telefono = c.Telefono,
                        email = c.Email,
                        cedula = c.Cedula,
                        activo = c.Activo,
                        totalFacturas = c.TotalFacturas,
                        fechaCreacion = c.FechaCreacion
                    }),
                    pagination = new
                    {
                        currentPage = resultado.CurrentPage,
                        totalPages = resultado.TotalPages,
                        totalItems = resultado.TotalItems,
                        pageSize = resultado.PageSize
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener clientes" });
            }
        }

        /// <summary>
        /// Obtener un cliente por ID - GET /api/movil/clientes/{id}
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var cliente = _clienteService.ObtenerPorId(id);
                
                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                var servicios = _clienteService.ObtenerServiciosActivos(id);

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
                        totalFacturas = cliente.TotalFacturas,
                        observaciones = cliente.Observaciones,
                        fechaCreacion = cliente.FechaCreacion,
                        servicios = servicios.Select(cs => new
                        {
                            id = cs.Servicio?.Id,
                            nombre = cs.Servicio?.Nombre,
                            precio = cs.Servicio?.Precio,
                            categoria = cs.Servicio?.Categoria
                        })
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener cliente" });
            }
        }

        /// <summary>
        /// Buscar clientes - GET /api/movil/clientes/buscar?q=texto
        /// </summary>
        [HttpGet("buscar")]
        public IActionResult Buscar([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new { success = false, message = "El término de búsqueda es requerido" });
                }

                var clientes = _clienteService.Buscar(q);

                return Ok(new
                {
                    success = true,
                    data = clientes.Select(c => new
                    {
                        id = c.Id,
                        codigo = c.Codigo,
                        nombre = c.Nombre,
                        telefono = c.Telefono,
                        activo = c.Activo
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al buscar clientes" });
            }
        }

        /// <summary>
        /// Crear un nuevo cliente - POST /api/movil/clientes
        /// </summary>
        [HttpPost]
        public IActionResult Create([FromBody] ClienteCreateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Nombre))
                {
                    return BadRequest(new { success = false, message = "El nombre es requerido" });
                }

                var cliente = new Models.Entities.Cliente
                {
                    Nombre = request.Nombre,
                    Telefono = request.Telefono,
                    Email = request.Email,
                    Cedula = request.Cedula,
                    Activo = request.Activo ?? true,
                    Observaciones = request.Observaciones
                };

                var clienteCreado = _clienteService.Crear(cliente);

                return CreatedAtAction(nameof(GetById), new { id = clienteCreado.Id }, new
                {
                    success = true,
                    message = "Cliente creado exitosamente",
                    data = new
                    {
                        id = clienteCreado.Id,
                        codigo = clienteCreado.Codigo,
                        nombre = clienteCreado.Nombre
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al crear cliente" });
            }
        }

        /// <summary>
        /// Actualizar un cliente - PUT /api/movil/clientes/{id}
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] ClienteUpdateRequest request)
        {
            try
            {
                var clienteExistente = _clienteService.ObtenerPorId(id);
                
                if (clienteExistente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                clienteExistente.Nombre = request.Nombre ?? clienteExistente.Nombre;
                clienteExistente.Telefono = request.Telefono ?? clienteExistente.Telefono;
                clienteExistente.Email = request.Email ?? clienteExistente.Email;
                clienteExistente.Cedula = request.Cedula ?? clienteExistente.Cedula;
                clienteExistente.Activo = request.Activo ?? clienteExistente.Activo;
                clienteExistente.Observaciones = request.Observaciones ?? clienteExistente.Observaciones;

                _clienteService.Actualizar(clienteExistente);

                return Ok(new
                {
                    success = true,
                    message = "Cliente actualizado exitosamente"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al actualizar cliente" });
            }
        }

        /// <summary>
        /// Eliminar un cliente - DELETE /api/movil/clientes/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var cliente = _clienteService.ObtenerPorId(id);
                
                if (cliente == null)
                {
                    return NotFound(new { success = false, message = "Cliente no encontrado" });
                }

                var eliminado = _clienteService.Eliminar(id);

                if (!eliminado)
                {
                    return BadRequest(new { success = false, message = "No se pudo eliminar. El cliente puede tener facturas asociadas." });
                }

                return Ok(new
                {
                    success = true,
                    message = "Cliente eliminado exitosamente"
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al eliminar cliente" });
            }
        }

        /// <summary>
        /// Obtener estadísticas de clientes - GET /api/movil/clientes/estadisticas
        /// </summary>
        [HttpGet("estadisticas")]
        public IActionResult GetEstadisticas()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = _clienteService.ObtenerTotal(),
                        activos = _clienteService.ObtenerTotalActivos(),
                        nuevosEsteMes = _clienteService.ObtenerNuevosEsteMes()
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener estadísticas" });
            }
        }
    }

    public class ClienteCreateRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Cedula { get; set; }
        public bool? Activo { get; set; }
        public string? Observaciones { get; set; }
    }

    public class ClienteUpdateRequest
    {
        public string? Nombre { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Cedula { get; set; }
        public bool? Activo { get; set; }
        public string? Observaciones { get; set; }
    }
}

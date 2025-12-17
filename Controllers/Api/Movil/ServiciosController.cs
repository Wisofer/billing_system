using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using billing_system.Services.IServices;

namespace billing_system.Controllers.Api.Movil
{
    [Route("api/movil/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ServiciosController : ControllerBase
    {
        private readonly IServicioService _servicioService;

        public ServiciosController(IServicioService servicioService)
        {
            _servicioService = servicioService;
        }

        /// <summary>
        /// Obtener todos los servicios - GET /api/movil/servicios
        /// </summary>
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? categoria = null, [FromQuery] bool? activos = true)
        {
            try
            {
                var servicios = activos == true 
                    ? _servicioService.ObtenerActivos() 
                    : _servicioService.ObtenerTodos();

                if (!string.IsNullOrWhiteSpace(categoria))
                {
                    servicios = servicios.Where(s => s.Categoria == categoria).ToList();
                }

                return Ok(new
                {
                    success = true,
                    data = servicios.Select(s => new
                    {
                        id = s.Id,
                        nombre = s.Nombre,
                        descripcion = s.Descripcion,
                        precio = s.Precio,
                        categoria = s.Categoria,
                        activo = s.Activo
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener servicios" });
            }
        }

        /// <summary>
        /// Obtener un servicio por ID - GET /api/movil/servicios/{id}
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var servicio = _servicioService.ObtenerPorId(id);

                if (servicio == null)
                {
                    return NotFound(new { success = false, message = "Servicio no encontrado" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = servicio.Id,
                        nombre = servicio.Nombre,
                        descripcion = servicio.Descripcion,
                        precio = servicio.Precio,
                        categoria = servicio.Categoria,
                        activo = servicio.Activo
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener servicio" });
            }
        }

        /// <summary>
        /// Obtener servicios por categoría - GET /api/movil/servicios/categoria/{categoria}
        /// </summary>
        [HttpGet("categoria/{categoria}")]
        public IActionResult GetByCategoria(string categoria)
        {
            try
            {
                var servicios = _servicioService.ObtenerActivosPorCategoria(categoria);

                return Ok(new
                {
                    success = true,
                    data = servicios.Select(s => new
                    {
                        id = s.Id,
                        nombre = s.Nombre,
                        descripcion = s.Descripcion,
                        precio = s.Precio,
                        categoria = s.Categoria
                    })
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener servicios por categoría" });
            }
        }

        /// <summary>
        /// Obtener categorías disponibles - GET /api/movil/servicios/categorias
        /// </summary>
        [HttpGet("categorias")]
        public IActionResult GetCategorias()
        {
            try
            {
                var servicios = _servicioService.ObtenerActivos();
                var categorias = servicios.Select(s => s.Categoria)
                                         .Distinct()
                                         .ToList();

                return Ok(new
                {
                    success = true,
                    data = categorias
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Error al obtener categorías" });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using billing_system.Services.IServices;
using billing_system.Models.Entities;
using System.Text.Json.Serialization;

namespace billing_system.Controllers.Api
{
    [Route("api/landing/[controller]")]
    [ApiController]
    public class ContactoController : ControllerBase
    {
        private readonly IContactoService _contactoService;

        public ContactoController(IContactoService contactoService)
        {
            _contactoService = contactoService;
        }

        /// <summary>
        /// Recibe un mensaje de contacto desde la landing page
        /// POST /api/landing/contacto
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje([FromBody] ContactoRequest request)
        {
            try
            {
                // Validar campos requeridos
                if (request == null)
                    return BadRequest(new { success = false, message = "El request está vacío" });

                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new { success = false, message = "El nombre es requerido" });

                if (string.IsNullOrWhiteSpace(request.Correo))
                    return BadRequest(new { success = false, message = "El correo es requerido" });

                if (string.IsNullOrWhiteSpace(request.Telefono))
                    return BadRequest(new { success = false, message = "El teléfono es requerido" });

                if (string.IsNullOrWhiteSpace(request.Mensaje))
                    return BadRequest(new { success = false, message = "El mensaje es requerido" });

                // Validar formato de correo
                if (!IsValidEmail(request.Correo))
                    return BadRequest(new { success = false, message = "El formato del correo no es válido" });

                // Convertir double a decimal para las coordenadas
                decimal? latitudDecimal = null;
                decimal? longitudDecimal = null;

                if (request.Latitud.HasValue)
                {
                    latitudDecimal = Convert.ToDecimal(request.Latitud.Value);
                }

                if (request.Longitud.HasValue)
                {
                    longitudDecimal = Convert.ToDecimal(request.Longitud.Value);
                }

                // Crear el contacto
                var contacto = new Contacto
                {
                    Nombre = request.Nombre.Trim(),
                    Correo = request.Correo.Trim().ToLower(),
                    Telefono = request.Telefono.Trim(),
                    Mensaje = request.Mensaje.Trim(),
                    Ubicacion = string.IsNullOrWhiteSpace(request.Ubicacion) ? null : request.Ubicacion.Trim(),
                    Latitud = latitudDecimal,
                    Longitud = longitudDecimal
                };

                await _contactoService.Crear(contacto);

                return Ok(new { 
                    success = true, 
                    message = "Mensaje enviado correctamente. Nos pondremos en contacto contigo pronto." 
                });
            }
            catch
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "Error al enviar el mensaje. Por favor, intenta nuevamente." 
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }
    }

    public class ContactoRequest
    {
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;
        
        [JsonPropertyName("correo")]
        public string Correo { get; set; } = string.Empty;
        
        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = string.Empty;
        
        [JsonPropertyName("mensaje")]
        public string Mensaje { get; set; } = string.Empty;
        
        [JsonPropertyName("ubicacion")]
        public string? Ubicacion { get; set; }
        
        [JsonPropertyName("latitud")]
        public double? Latitud { get; set; }
        
        [JsonPropertyName("longitud")]
        public double? Longitud { get; set; }
    }
}

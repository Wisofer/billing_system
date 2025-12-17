using System.ComponentModel.DataAnnotations;

namespace billing_system.Models.Entities
{
    public class Contacto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido")]
        [StringLength(150, ErrorMessage = "El correo no puede exceder 150 caracteres")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es requerido")]
        [StringLength(1000, ErrorMessage = "El mensaje no puede exceder 1000 caracteres")]
        public string Mensaje { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        [StringLength(20)]
        public string Estado { get; set; } = "Nuevo"; // Nuevo, Leído, Respondido

        public DateTime? FechaLeido { get; set; }
        
        public DateTime? FechaRespondido { get; set; }
    }
}


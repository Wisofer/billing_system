using billing_system.Models.Entities;
using Microsoft.Extensions.Logging;

namespace billing_system.Data;

/// <summary>
/// Inicializa los servicios de landing page por defecto
/// </summary>
public static class InicializarServiciosLandingPage
{
    public static void CrearServiciosLandingPageDefectoSiNoExisten(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si ya existen servicios
            if (context.ServiciosLandingPage.Any())
            {
                logger.LogInformation("Ya existen servicios de landing page en la base de datos.");
                return;
            }

            logger.LogInformation("Creando servicios de landing page por defecto...");

            var servicios = new List<ServicioLandingPage>
            {
                // Plan Básico
                new ServicioLandingPage
                {
                    Titulo = "Plan de hasta 10Mbps",
                    Descripcion = "Servicio de Internet Residencial hasta 10mbps.",
                    Precio = 920.00m,
                    Velocidad = "10Mbps",
                    Icono = "📡",
                    Orden = 1,
                    Activo = true,
                    Destacado = false,
                    FechaCreacion = DateTime.UtcNow
                },
                // Plan Intermedio
                new ServicioLandingPage
                {
                    Titulo = "Plan de hasta 10Mbps",
                    Descripcion = "Servicio de Internet Residencial hasta 10Mbps",
                    Precio = 1104.00m,
                    Velocidad = "10Mbps",
                    Icono = "🌐",
                    Orden = 2,
                    Activo = true,
                    Destacado = false,
                    FechaCreacion = DateTime.UtcNow
                },
                // Plan RE
                new ServicioLandingPage
                {
                    Titulo = "Plan de hasta 10Mbps",
                    Descripcion = "Servicio de Internet Residencial RE hasta 10Mbps",
                    Precio = 1288.00m,
                    Velocidad = "10Mbps RE",
                    Icono = "🚀",
                    Orden = 3,
                    Activo = true,
                    Destacado = false,
                    FechaCreacion = DateTime.UtcNow
                },
                // Plan Especial
                new ServicioLandingPage
                {
                    Titulo = "Plan de hasta 10Mbps",
                    Descripcion = "Servicio de Internet Residencial hasta 10Mbps",
                    Precio = 1000.00m,
                    Velocidad = "10Mbps",
                    Icono = "⚡",
                    Etiqueta = "ESPECIAL",
                    ColorEtiqueta = "bg-green-500",
                    Orden = 4,
                    Activo = true,
                    Destacado = false,
                    FechaCreacion = DateTime.UtcNow
                },
                // Plan Oferta Diciembre
                new ServicioLandingPage
                {
                    Titulo = "Plan de hasta 20Mbps",
                    Descripcion = "Servicio de Internet Residencial RE hasta 20Mbps",
                    Precio = 1288.00m,
                    Velocidad = "20Mbps RE",
                    Icono = "🎄",
                    Etiqueta = "OFERTA DICIEMBRE",
                    ColorEtiqueta = "bg-red-500",
                    Orden = 5,
                    Activo = true,
                    Destacado = true,
                    FechaCreacion = DateTime.UtcNow
                }
            };

            context.ServiciosLandingPage.AddRange(servicios);
            context.SaveChanges();

            logger.LogInformation($"✅ {servicios.Count} servicios de landing page creados exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear servicios de landing page por defecto");
        }
    }
}


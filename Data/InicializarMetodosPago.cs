using billing_system.Models.Entities;
using Microsoft.Extensions.Logging;

namespace billing_system.Data;

/// <summary>
/// Inicializa los métodos de pago por defecto para la landing page
/// </summary>
public static class InicializarMetodosPago
{
    public static void CrearMetodosPagoDefectoSiNoExisten(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si ya existen métodos de pago
            if (context.MetodosPago.Any())
            {
                logger.LogInformation("Ya existen métodos de pago en la base de datos.");
                return;
            }

            logger.LogInformation("Creando métodos de pago por defecto...");

            var metodosPago = new List<MetodoPago>
            {
                // Banpro - Córdobas
                new MetodoPago
                {
                    NombreBanco = "Banpro",
                    Icono = "🏦",
                    TipoCuenta = "Córdobas",
                    Moneda = "C$",
                    NumeroCuenta = "10020200333635",
                    Orden = 1,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                // Banpro - Dólares
                new MetodoPago
                {
                    NombreBanco = "Banpro",
                    Icono = "🏦",
                    TipoCuenta = "Dólares",
                    Moneda = "$",
                    NumeroCuenta = "10020210146151",
                    Orden = 2,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                // Banpro - Billetera Móvil
                new MetodoPago
                {
                    NombreBanco = "Banpro",
                    Icono = "🏦",
                    TipoCuenta = "Billetera Móvil",
                    Moneda = "📱",
                    NumeroCuenta = "89308058",
                    Orden = 3,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                // Lafise - Córdobas
                new MetodoPago
                {
                    NombreBanco = "Lafise",
                    Icono = "🏛️",
                    TipoCuenta = "Córdobas",
                    Moneda = "C$",
                    NumeroCuenta = "134098622",
                    Orden = 4,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                // Lafise - Dólares
                new MetodoPago
                {
                    NombreBanco = "Lafise",
                    Icono = "🏛️",
                    TipoCuenta = "Dólares",
                    Moneda = "$",
                    NumeroCuenta = "131247706",
                    Orden = 5,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                },
                // BAC - Próximamente
                new MetodoPago
                {
                    NombreBanco = "BAC",
                    Icono = "💳",
                    TipoCuenta = "Próximamente",
                    Moneda = "",
                    NumeroCuenta = null,
                    Mensaje = "Próximamente Disponible",
                    Orden = 6,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                }
            };

            context.MetodosPago.AddRange(metodosPago);
            context.SaveChanges();

            logger.LogInformation($"✅ {metodosPago.Count} métodos de pago creados exitosamente.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear métodos de pago por defecto");
        }
    }
}


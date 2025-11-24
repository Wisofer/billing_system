using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using billing_system.Models.Entities;

namespace billing_system.Data;

public static class MigrateClienteServicios
{
    /// <summary>
    /// Migra los ServicioId existentes en Clientes a la nueva tabla ClienteServicios
    /// </summary>
    public static void MigrateServiciosToClienteServicios(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Iniciando migración de ServicioId a ClienteServicios...");

            // Obtener todos los clientes que tienen un ServicioId asignado
            var clientesConServicio = context.Clientes
                .Where(c => c.ServicioId.HasValue)
                .ToList();

            if (!clientesConServicio.Any())
            {
                logger.LogInformation("No hay clientes con servicios asignados para migrar.");
                return;
            }

            logger.LogInformation($"Se encontraron {clientesConServicio.Count} clientes con servicios para migrar.");

            int migrados = 0;
            int errores = 0;
            int yaExistentes = 0;

            foreach (var cliente in clientesConServicio)
            {
                try
                {
                    // Verificar si el servicio existe y está activo
                    var servicioId = cliente.ServicioId.Value;
                    var servicio = context.Servicios.FirstOrDefault(s => s.Id == servicioId);
                    if (servicio == null || !servicio.Activo)
                    {
                        logger.LogWarning($"Servicio ID {servicioId} no existe o no está activo para cliente {cliente.Nombre}. Se omite.");
                        errores++;
                        continue;
                    }

                    // Verificar si ya existe un ClienteServicio para este cliente y servicio
                    var existe = context.ClienteServicios
                        .Any(cs => cs.ClienteId == cliente.Id && cs.ServicioId == servicioId);

                    if (existe)
                    {
                        yaExistentes++;
                        continue;
                    }

                    // Crear nuevo ClienteServicio
                    var clienteServicio = new ClienteServicio
                    {
                        ClienteId = cliente.Id,
                        ServicioId = servicioId,
                        Activo = true,
                        FechaInicio = cliente.FechaCreacion, // Usar la fecha de creación del cliente como fecha de inicio
                        FechaCreacion = DateTime.Now
                    };

                    context.ClienteServicios.Add(clienteServicio);
                    migrados++;

                    // Guardar en lotes de 50 para mejor rendimiento
                    if (migrados % 50 == 0)
                    {
                        context.SaveChanges();
                        logger.LogInformation($"Progreso: {migrados} servicios migrados hasta ahora...");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error al migrar servicio para cliente ID {cliente.Id}: {cliente.Nombre}");
                    errores++;
                }
            }

            // Guardar los servicios restantes
            if (migrados % 50 != 0)
            {
                context.SaveChanges();
            }

            logger.LogInformation($"✅ Migración completada: {migrados} servicios migrados, {yaExistentes} ya existentes, {errores} errores.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la migración de ClienteServicios");
            throw;
        }
    }
}


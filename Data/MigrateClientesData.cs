using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using billing_system.Models.Entities;
using System.Data.Common;

namespace billing_system.Data;

public static class MigrateClientesData
{
    /// <summary>
    /// Migra los datos de la tabla antigua 'clientes' (minúscula) a la nueva tabla 'Clientes' (mayúscula)
    /// </summary>
    public static void MigrateOldClientesToNew(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            logger.LogInformation("Iniciando migración de datos de 'clientes' a 'Clientes'...");

            // Verificar si existe la tabla antigua usando SQL directo
            var connection = context.Database.GetDbConnection();
            connection.Open();
            
            bool tableExists = false;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'clientes'";
                var result = command.ExecuteScalar();
                tableExists = result != null && Convert.ToInt32(result) > 0;
            }

            if (!tableExists)
            {
                logger.LogInformation("La tabla 'clientes' no existe. No hay datos para migrar.");
                connection.Close();
                return;
            }

            // Obtener todos los clientes de la tabla antigua
            var oldClientes = new List<OldCliente>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, nombre, cedula, telefono, facturas, fecha_registro FROM clientes ORDER BY id";
                using (var reader = command.ExecuteReader())
                {
                    var idOrdinal = reader.GetOrdinal("id");
                    var nombreOrdinal = reader.GetOrdinal("nombre");
                    var cedulaOrdinal = reader.GetOrdinal("cedula");
                    var telefonoOrdinal = reader.GetOrdinal("telefono");
                    var facturasOrdinal = reader.GetOrdinal("facturas");
                    var fechaRegistroOrdinal = reader.GetOrdinal("fecha_registro");

                    while (reader.Read())
                    {
                        oldClientes.Add(new OldCliente
                        {
                            Id = reader.GetInt32(idOrdinal),
                            Nombre = reader.IsDBNull(nombreOrdinal) ? "" : reader.GetString(nombreOrdinal),
                            Cedula = reader.IsDBNull(cedulaOrdinal) ? null : reader.GetString(cedulaOrdinal),
                            Telefono = reader.IsDBNull(telefonoOrdinal) ? null : reader.GetString(telefonoOrdinal),
                            Facturas = reader.IsDBNull(facturasOrdinal) ? 0 : reader.GetInt32(facturasOrdinal),
                            FechaRegistro = reader.IsDBNull(fechaRegistroOrdinal) ? DateTime.Now : reader.GetDateTime(fechaRegistroOrdinal)
                        });
                    }
                }
            }

            connection.Close();

            if (oldClientes.Count == 0)
            {
                logger.LogInformation("No hay clientes en la tabla antigua para migrar.");
                return;
            }

            logger.LogInformation($"Se encontraron {oldClientes.Count} clientes para migrar.");

            // Obtener el último código generado en la tabla nueva
            var existingClientes = context.Clientes
                .Where(c => c.Codigo.StartsWith("CLI-"))
                .Select(c => c.Codigo)
                .ToList()
                .Select(c =>
                {
                    var partes = c.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[1], out var num))
                        return num;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            int nextCodigoNumero = existingClientes + 1;
            int migrados = 0;
            int duplicados = 0;
            int errores = 0;

            // Obtener todos los códigos existentes de una vez para mejor rendimiento
            var codigosExistentes = context.Clientes.Select(c => c.Codigo).ToHashSet();
            
            // Crear un diccionario de cédulas existentes para verificación rápida
            // Solo considerar duplicado si tiene cédula y ya existe
            var cedulasExistentes = context.Clientes
                .Where(c => !string.IsNullOrEmpty(c.Cedula) && c.Cedula.Trim() != "")
                .Select(c => c.Cedula!.Trim().ToUpper())
                .ToHashSet();
            
            logger.LogInformation($"Clientes existentes en 'Clientes': {context.Clientes.Count()}");
            logger.LogInformation($"Cédulas únicas existentes: {cedulasExistentes.Count}");

            // Verificar si los clientes de la tabla antigua ya están en la nueva
            // Comparando por cédula (más confiable)
            var clientesYaMigrados = 0;
            foreach (var oldCliente in oldClientes)
            {
                var cedulaOld = string.IsNullOrWhiteSpace(oldCliente.Cedula) 
                    ? null 
                    : oldCliente.Cedula.Trim().ToUpper();
                
                if (cedulaOld != null && cedulaOld != "" && cedulasExistentes.Contains(cedulaOld))
                {
                    clientesYaMigrados++;
                }
            }
            
            logger.LogInformation($"Clientes que ya están migrados (por cédula): {clientesYaMigrados} de {oldClientes.Count}");

            foreach (var oldCliente in oldClientes)
            {
                try
                {
                    var nombreNormalizado = oldCliente.Nombre.Trim();
                    var cedulaNormalizada = string.IsNullOrWhiteSpace(oldCliente.Cedula) 
                        ? null 
                        : oldCliente.Cedula.Trim().ToUpper();

                    // Solo considerar duplicado si tiene cédula y ya existe en la tabla Clientes
                    // Esto es más confiable que comparar nombres (pueden haber nombres similares pero personas diferentes)
                    var existePorCedula = cedulaNormalizada != null && 
                                         cedulaNormalizada != "" && 
                                         cedulasExistentes.Contains(cedulaNormalizada);

                    if (existePorCedula)
                    {
                        logger.LogWarning($"Cliente duplicado (por cédula): {nombreNormalizado} (Cédula: {cedulaNormalizada}). Se omite.");
                        duplicados++;
                        continue;
                    }

                    // Generar código único
                    string codigo;
                    do
                    {
                        codigo = $"CLI-{nextCodigoNumero:D3}";
                        nextCodigoNumero++;
                    } while (codigosExistentes.Contains(codigo));

                    codigosExistentes.Add(codigo);

                    // Crear nuevo cliente
                    var nuevoCliente = new Cliente
                    {
                        Codigo = codigo,
                        Nombre = oldCliente.Nombre.Trim(),
                        Cedula = cedulaNormalizada,
                        Telefono = string.IsNullOrWhiteSpace(oldCliente.Telefono) ? null : oldCliente.Telefono.Trim(),
                        Email = null, // No existe en la tabla antigua
                        Activo = true, // Por defecto activo
                        FechaCreacion = oldCliente.FechaRegistro // Preservar la fecha de registro original
                    };

                    context.Clientes.Add(nuevoCliente);
                    if (cedulaNormalizada != null && cedulaNormalizada != "")
                    {
                        cedulasExistentes.Add(cedulaNormalizada);
                    }

                    migrados++;

                    // Guardar en lotes de 50 para mejor rendimiento
                    if (migrados % 50 == 0)
                    {
                        context.SaveChanges();
                        logger.LogInformation($"Progreso: {migrados} clientes migrados hasta ahora...");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error al migrar cliente ID {oldCliente.Id}: {oldCliente.Nombre}");
                    errores++;
                }
            }

            // Guardar los clientes restantes
            if (migrados % 50 != 0)
            {
                context.SaveChanges();
            }

            logger.LogInformation($"✅ Migración completada: {migrados} clientes migrados, {duplicados} duplicados omitidos, {errores} errores.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error durante la migración de clientes");
            throw;
        }
    }

    /// <summary>
    /// Clase temporal para mapear los datos de la tabla antigua
    /// </summary>
    private class OldCliente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Cedula { get; set; }
        public string? Telefono { get; set; }
        public int Facturas { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}


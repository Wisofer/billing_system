using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Data;

public static class InicializarUsuarioDemo
{
    public static void CrearDemoSiNoExiste(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si ya existe un usuario demo
            var existeDemo = context.Usuarios
                .Any(u => u.NombreUsuario.ToLower() == "demo");

            if (existeDemo)
            {
                logger.LogInformation("El usuario demo ya existe en la base de datos.");
                return;
            }

            // Crear usuario demo
            var demo = new Usuario
            {
                NombreUsuario = "demo",
                Contrasena = PasswordHelper.HashPassword("demo"),
                NombreCompleto = "Usuario Demo",
                Rol = SD.RolDemo,
                Activo = true
            };

            context.Usuarios.Add(demo);
            context.SaveChanges();

            logger.LogInformation("Usuario demo creado exitosamente. Credenciales: demo/demo");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear el usuario demo en la base de datos.");
        }
    }
}



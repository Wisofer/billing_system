using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace billing_system.Scripts;

public class CrearUsuarioDemoManual
{
    public static void Ejecutar()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        var configuration = builder.Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: No se encontró la cadena de conexión 'DefaultConnection'");
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        using var context = new ApplicationDbContext(optionsBuilder.Options);
        
        // Crear un logger simple para la consola
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            // Verificar si existe el usuario demo
            var existeDemo = context.Usuarios.Any(u => u.NombreUsuario.ToLower() == "demo");

            if (existeDemo)
            {
                var demo = context.Usuarios.First(u => u.NombreUsuario.ToLower() == "demo");
                Console.WriteLine($"Usuario demo ya existe:");
                Console.WriteLine($"  ID: {demo.Id}");
                Console.WriteLine($"  NombreUsuario: {demo.NombreUsuario}");
                Console.WriteLine($"  Rol: {demo.Rol}");
                Console.WriteLine($"  Activo: {demo.Activo}");
                
                // Verificar contraseña
                var hashEsperado = PasswordHelper.HashPassword("demo");
                var hashCoincide = hashEsperado == demo.Contrasena;
                Console.WriteLine($"  Hash coincide: {hashCoincide}");
                
                if (!hashCoincide)
                {
                    Console.WriteLine("  ACTUALIZANDO contraseña...");
                    demo.Contrasena = hashEsperado;
                    context.SaveChanges();
                    Console.WriteLine("  Contraseña actualizada correctamente!");
                }
            }
            else
            {
                Console.WriteLine("Usuario demo NO existe. Creando...");
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
                Console.WriteLine("Usuario demo creado exitosamente!");
                Console.WriteLine("Credenciales: demo/demo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}


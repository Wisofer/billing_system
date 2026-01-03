using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace billing_system.Data;

public static class InicializarInventario
{
    public static void InicializarDatosBasicos(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Inicializar categorías básicas
            if (!context.CategoriasEquipo.Any())
            {
                logger.LogInformation("Inicializando categorías de equipos...");
                
                var categorias = new List<CategoriaEquipo>
                {
                    new CategoriaEquipo { Nombre = "Router", Descripcion = "Routers y equipos de red", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Antena", Descripcion = "Antenas y accesorios", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Cable", Descripcion = "Cables y conectores", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Switch", Descripcion = "Switches de red", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Access Point", Descripcion = "Puntos de acceso WiFi", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Modem", Descripcion = "Módems", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Herramienta", Descripcion = "Herramientas de instalación", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Consumible", Descripcion = "Materiales consumibles", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Ferretería", Descripcion = "Materiales de ferretería para instalaciones (tubos, platinas, tornillos, etc.)", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Repuesto", Descripcion = "Repuestos y componentes", Activo = true, FechaCreacion = DateTime.Now },
                    new CategoriaEquipo { Nombre = "Otro", Descripcion = "Otros equipos", Activo = true, FechaCreacion = DateTime.Now }
                };
                
                context.CategoriasEquipo.AddRange(categorias);
                context.SaveChanges();
                logger.LogInformation("Categorías de equipos inicializadas correctamente.");
            }

            // Inicializar ubicaciones básicas
            if (!context.Ubicaciones.Any())
            {
                logger.LogInformation("Inicializando ubicaciones...");
                
                var ubicaciones = new List<Ubicacion>
                {
                    new Ubicacion { Nombre = "Almacén Principal", Direccion = "", Tipo = SD.TipoUbicacionAlmacen, Activo = true, FechaCreacion = DateTime.Now },
                    new Ubicacion { Nombre = "Almacén Secundario", Direccion = "", Tipo = SD.TipoUbicacionAlmacen, Activo = true, FechaCreacion = DateTime.Now },
                    new Ubicacion { Nombre = "En Campo", Direccion = "", Tipo = SD.TipoUbicacionCampo, Activo = true, FechaCreacion = DateTime.Now },
                    new Ubicacion { Nombre = "En Reparación", Direccion = "", Tipo = SD.TipoUbicacionReparacion, Activo = true, FechaCreacion = DateTime.Now }
                };
                
                context.Ubicaciones.AddRange(ubicaciones);
                context.SaveChanges();
                logger.LogInformation("Ubicaciones inicializadas correctamente.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al inicializar datos básicos de inventario");
        }
    }
}


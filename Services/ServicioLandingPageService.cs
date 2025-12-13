using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class ServicioLandingPageService : IServicioLandingPageService
{
    private readonly ApplicationDbContext _context;
    private static bool _columnaMensajeVerificada = false;

    public ServicioLandingPageService(ApplicationDbContext context)
    {
        _context = context;
        VerificarYAgregarColumnaMensaje();
    }

    private void VerificarYAgregarColumnaMensaje()
    {
        if (_columnaMensajeVerificada) return;

        try
        {
            // Verificar si la columna existe
            var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT COUNT(*) 
                    FROM information_schema.columns 
                    WHERE table_name = 'ServiciosLandingPage' 
                    AND column_name = 'Mensaje';
                ";
                var existe = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!existe)
                {
                    // Agregar la columna
                    _context.Database.ExecuteSqlRaw(@"
                        ALTER TABLE ""ServiciosLandingPage"" 
                        ADD COLUMN ""Mensaje"" character varying(500) NULL;
                    ");
                }
            }

            _columnaMensajeVerificada = true;
        }
        catch
        {
            // Si falla, intentar de nuevo en la próxima vez
            _columnaMensajeVerificada = false;
        }
    }

    public List<ServicioLandingPage> ObtenerTodos()
    {
        return _context.ServiciosLandingPage
            .OrderBy(s => s.Orden)
            .ThenBy(s => s.Precio)
            .ToList();
    }

    public List<ServicioLandingPage> ObtenerActivos()
    {
        return _context.ServiciosLandingPage
            .Where(s => s.Activo)
            .OrderBy(s => s.Orden)
            .ThenBy(s => s.Precio)
            .ToList();
    }

    public List<ServicioLandingPage> ObtenerActivosOrdenados()
    {
        return _context.ServiciosLandingPage
            .Where(s => s.Activo)
            .OrderBy(s => s.Orden)
            .ThenBy(s => s.Precio)
            .ToList();
    }

    public ServicioLandingPage? ObtenerPorId(int id)
    {
        return _context.ServiciosLandingPage.FirstOrDefault(s => s.Id == id);
    }

    public ServicioLandingPage Crear(ServicioLandingPage servicio)
    {
        servicio.FechaCreacion = DateTime.UtcNow;
        _context.ServiciosLandingPage.Add(servicio);
        _context.SaveChanges();
        return servicio;
    }

    public bool Actualizar(ServicioLandingPage servicio)
    {
        try
        {
            var existente = _context.ServiciosLandingPage.Find(servicio.Id);
            if (existente == null)
                return false;

            existente.Titulo = servicio.Titulo;
            existente.Descripcion = servicio.Descripcion;
            existente.Precio = servicio.Precio;
            existente.Velocidad = servicio.Velocidad;
            existente.Etiqueta = servicio.Etiqueta;
            existente.ColorEtiqueta = servicio.ColorEtiqueta;
            existente.Icono = servicio.Icono;
            existente.Caracteristicas = servicio.Caracteristicas;
            existente.Mensaje = servicio.Mensaje;
            existente.Orden = servicio.Orden;
            existente.Activo = servicio.Activo;
            existente.Destacado = servicio.Destacado;
            existente.FechaActualizacion = DateTime.UtcNow;

            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Eliminar(int id)
    {
        try
        {
            var servicio = _context.ServiciosLandingPage.Find(id);
            if (servicio == null)
                return false;

            _context.ServiciosLandingPage.Remove(servicio);
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ActualizarOrden(Dictionary<int, int> ordenPorId)
    {
        try
        {
            foreach (var kvp in ordenPorId)
            {
                var servicio = _context.ServiciosLandingPage.Find(kvp.Key);
                if (servicio != null)
                {
                    servicio.Orden = kvp.Value;
                    servicio.FechaActualizacion = DateTime.UtcNow;
                }
            }
            _context.SaveChanges();
            return true;
        }
        catch
        {
            return false;
        }
    }
}


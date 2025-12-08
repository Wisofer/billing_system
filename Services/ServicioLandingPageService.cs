using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;

namespace billing_system.Services;

public class ServicioLandingPageService : IServicioLandingPageService
{
    private readonly ApplicationDbContext _context;

    public ServicioLandingPageService(ApplicationDbContext context)
    {
        _context = context;
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


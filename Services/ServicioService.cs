using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;

namespace billing_system.Services;

public class ServicioService : IServicioService
{
    public List<Servicio> ObtenerTodos()
    {
        return InMemoryStorage.Servicios.ToList();
    }

    public List<Servicio> ObtenerActivos()
    {
        return InMemoryStorage.Servicios.Where(s => s.Activo).ToList();
    }

    public Servicio? ObtenerPorId(int id)
    {
        return InMemoryStorage.Servicios.FirstOrDefault(s => s.Id == id);
    }

    public Servicio Crear(Servicio servicio)
    {
        servicio.Id = InMemoryStorage.GetNextServicioId();
        servicio.FechaCreacion = DateTime.Now;
        servicio.Activo = true;
        InMemoryStorage.Servicios.Add(servicio);
        return servicio;
    }

    public Servicio Actualizar(Servicio servicio)
    {
        var existente = ObtenerPorId(servicio.Id);
        if (existente == null)
            throw new Exception("Servicio no encontrado");

        existente.Nombre = servicio.Nombre;
        existente.Precio = servicio.Precio;
        existente.Activo = servicio.Activo;

        return existente;
    }

    public bool Eliminar(int id)
    {
        var servicio = ObtenerPorId(id);
        if (servicio == null)
            return false;

        // Verificar si tiene facturas
        var tieneFacturas = InMemoryStorage.Facturas.Any(f => f.ServicioId == id);
        if (tieneFacturas)
            return false; // No se puede eliminar si tiene facturas

        InMemoryStorage.Servicios.Remove(servicio);
        return true;
    }
}


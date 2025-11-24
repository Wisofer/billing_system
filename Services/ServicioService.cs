using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class ServicioService : IServicioService
{
    private readonly ApplicationDbContext _context;

    public ServicioService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Servicio> ObtenerTodos()
    {
        return _context.Servicios.ToList();
    }

    public List<Servicio> ObtenerActivos()
    {
        return _context.Servicios.Where(s => s.Activo).ToList();
    }

    public Servicio? ObtenerPorId(int id)
    {
        return _context.Servicios.FirstOrDefault(s => s.Id == id);
    }

    public Servicio Crear(Servicio servicio)
    {
        servicio.FechaCreacion = DateTime.Now;
        servicio.Activo = true;
        _context.Servicios.Add(servicio);
        _context.SaveChanges();
        return servicio;
    }

    public Servicio Actualizar(Servicio servicio)
    {
        var existente = ObtenerPorId(servicio.Id);
        if (existente == null)
            throw new Exception("Servicio no encontrado");

        existente.Nombre = servicio.Nombre;
        existente.Descripcion = servicio.Descripcion;
        existente.Precio = servicio.Precio;
        existente.Activo = servicio.Activo;

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var servicio = ObtenerPorId(id);
        if (servicio == null)
            return false;

        // Verificar si tiene facturas
        var tieneFacturas = _context.Facturas.Any(f => f.ServicioId == id);
        if (tieneFacturas)
            return false; // No se puede eliminar si tiene facturas

        _context.Servicios.Remove(servicio);
        _context.SaveChanges();
        return true;
    }
}


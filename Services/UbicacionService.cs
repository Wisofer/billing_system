using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class UbicacionService : IUbicacionService
{
    private readonly ApplicationDbContext _context;

    public UbicacionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Ubicacion> ObtenerTodas()
    {
        return _context.Ubicaciones
            .OrderBy(u => u.Nombre)
            .ToList();
    }

    public List<Ubicacion> ObtenerActivas()
    {
        return _context.Ubicaciones
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .ToList();
    }

    public Ubicacion? ObtenerPorId(int id)
    {
        return _context.Ubicaciones
            .FirstOrDefault(u => u.Id == id);
    }

    public Ubicacion Crear(Ubicacion ubicacion)
    {
        if (ExisteNombre(ubicacion.Nombre))
        {
            throw new Exception($"Ya existe una ubicación con el nombre '{ubicacion.Nombre}'");
        }

        ubicacion.FechaCreacion = DateTime.Now;
        ubicacion.Activo = true;

        _context.Ubicaciones.Add(ubicacion);
        _context.SaveChanges();
        return ubicacion;
    }

    public Ubicacion Actualizar(Ubicacion ubicacion)
    {
        var existente = ObtenerPorId(ubicacion.Id);
        if (existente == null)
            throw new Exception("Ubicación no encontrada");

        if (ExisteNombre(ubicacion.Nombre, ubicacion.Id))
        {
            throw new Exception($"Ya existe otra ubicación con el nombre '{ubicacion.Nombre}'");
        }

        existente.Nombre = ubicacion.Nombre;
        existente.Direccion = ubicacion.Direccion;
        existente.Tipo = ubicacion.Tipo;
        existente.Activo = ubicacion.Activo;

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var ubicacion = ObtenerPorId(id);
        if (ubicacion == null)
            return false;

        // Verificar si tiene equipos activos asociados
        var tieneEquiposActivos = _context.Equipos.Any(e => e.UbicacionId == id && e.Activo);
        if (tieneEquiposActivos)
            return false; // No se puede eliminar si tiene equipos activos

        // Eliminar definitivamente los equipos inactivos asociados a esta ubicación
        var equiposInactivos = _context.Equipos
            .Where(e => e.UbicacionId == id && !e.Activo)
            .ToList();

        if (equiposInactivos.Count > 0)
        {
            _context.Equipos.RemoveRange(equiposInactivos);
        }

        _context.Ubicaciones.Remove(ubicacion);
        _context.SaveChanges();
        return true;
    }

    public bool ExisteNombre(string nombre, int? idExcluir = null)
    {
        return _context.Ubicaciones
            .Any(u => u.Nombre.ToLower() == nombre.ToLower() && (idExcluir == null || u.Id != idExcluir));
    }
}


using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class CategoriaEquipoService : ICategoriaEquipoService
{
    private readonly ApplicationDbContext _context;

    public CategoriaEquipoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<CategoriaEquipo> ObtenerTodas()
    {
        return _context.CategoriasEquipo
            .OrderBy(c => c.Nombre)
            .ToList();
    }

    public List<CategoriaEquipo> ObtenerActivas()
    {
        return _context.CategoriasEquipo
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ToList();
    }

    public CategoriaEquipo? ObtenerPorId(int id)
    {
        return _context.CategoriasEquipo
            .FirstOrDefault(c => c.Id == id);
    }

    public CategoriaEquipo Crear(CategoriaEquipo categoria)
    {
        if (ExisteNombre(categoria.Nombre))
        {
            throw new Exception($"Ya existe una categoría con el nombre '{categoria.Nombre}'");
        }

        categoria.FechaCreacion = DateTime.Now;
        categoria.Activo = true;

        _context.CategoriasEquipo.Add(categoria);
        _context.SaveChanges();
        return categoria;
    }

    public CategoriaEquipo Actualizar(CategoriaEquipo categoria)
    {
        var existente = ObtenerPorId(categoria.Id);
        if (existente == null)
            throw new Exception("Categoría no encontrada");

        if (ExisteNombre(categoria.Nombre, categoria.Id))
        {
            throw new Exception($"Ya existe otra categoría con el nombre '{categoria.Nombre}'");
        }

        existente.Nombre = categoria.Nombre;
        existente.Descripcion = categoria.Descripcion;
        existente.Activo = categoria.Activo;

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var categoria = ObtenerPorId(id);
        if (categoria == null)
            return false;

        // Verificar si tiene equipos activos asociados
        var tieneEquiposActivos = _context.Equipos.Any(e => e.CategoriaEquipoId == id && e.Activo);
        if (tieneEquiposActivos)
            return false; // No se puede eliminar si tiene equipos activos

        // Eliminar de forma definitiva los equipos inactivos asociados a esta categoría
        var equiposInactivos = _context.Equipos
            .Where(e => e.CategoriaEquipoId == id && !e.Activo)
            .ToList();

        if (equiposInactivos.Count > 0)
        {
            _context.Equipos.RemoveRange(equiposInactivos);
        }

        _context.CategoriasEquipo.Remove(categoria);
        _context.SaveChanges();
        return true;
    }

    public bool ExisteNombre(string nombre, int? idExcluir = null)
    {
        return _context.CategoriasEquipo
            .Any(c => c.Nombre.ToLower() == nombre.ToLower() && (idExcluir == null || c.Id != idExcluir));
    }
}


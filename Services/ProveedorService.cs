using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class ProveedorService : IProveedorService
{
    private readonly ApplicationDbContext _context;

    public ProveedorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Proveedor> ObtenerTodos()
    {
        return _context.Proveedores
            .OrderBy(p => p.Nombre)
            .ToList();
    }

    public List<Proveedor> ObtenerActivos()
    {
        return _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToList();
    }

    public Proveedor? ObtenerPorId(int id)
    {
        return _context.Proveedores
            .FirstOrDefault(p => p.Id == id);
    }

    public Proveedor Crear(Proveedor proveedor)
    {
        if (ExisteNombre(proveedor.Nombre))
        {
            throw new Exception($"Ya existe un proveedor con el nombre '{proveedor.Nombre}'");
        }

        proveedor.FechaCreacion = DateTime.Now;
        proveedor.Activo = true;

        _context.Proveedores.Add(proveedor);
        _context.SaveChanges();
        return proveedor;
    }

    public Proveedor Actualizar(Proveedor proveedor)
    {
        var existente = ObtenerPorId(proveedor.Id);
        if (existente == null)
            throw new Exception("Proveedor no encontrado");

        if (ExisteNombre(proveedor.Nombre, proveedor.Id))
        {
            throw new Exception($"Ya existe otro proveedor con el nombre '{proveedor.Nombre}'");
        }

        existente.Nombre = proveedor.Nombre;
        existente.Contacto = proveedor.Contacto;
        existente.Telefono = proveedor.Telefono;
        existente.Email = proveedor.Email;
        existente.Direccion = proveedor.Direccion;
        existente.Observaciones = proveedor.Observaciones;
        existente.Activo = proveedor.Activo;

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var proveedor = ObtenerPorId(id);
        if (proveedor == null)
            return false;

        // Verificar si tiene equipos asociados
        var tieneEquipos = _context.Equipos.Any(e => e.ProveedorId == id && e.Activo);
        if (tieneEquipos)
            return false; // No se puede eliminar si tiene equipos activos

        _context.Proveedores.Remove(proveedor);
        _context.SaveChanges();
        return true;
    }

    public bool ExisteNombre(string nombre, int? idExcluir = null)
    {
        return _context.Proveedores
            .Any(p => p.Nombre.ToLower() == nombre.ToLower() && (idExcluir == null || p.Id != idExcluir));
    }
}


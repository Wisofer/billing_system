using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class MetodoPagoService : IMetodoPagoService
{
    private readonly ApplicationDbContext _context;

    public MetodoPagoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<MetodoPago> ObtenerTodos()
    {
        return _context.MetodosPago
            .OrderBy(m => m.Orden)
            .ThenBy(m => m.NombreBanco)
            .ToList();
    }

    public List<MetodoPago> ObtenerActivos()
    {
        return _context.MetodosPago
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .ThenBy(m => m.NombreBanco)
            .ToList();
    }

    public List<MetodoPago> ObtenerActivosOrdenados()
    {
        return _context.MetodosPago
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .ThenBy(m => m.NombreBanco)
            .ToList();
    }

    public MetodoPago? ObtenerPorId(int id)
    {
        return _context.MetodosPago.FirstOrDefault(m => m.Id == id);
    }

    public MetodoPago Crear(MetodoPago metodoPago)
    {
        metodoPago.FechaCreacion = DateTime.UtcNow;
        _context.MetodosPago.Add(metodoPago);
        _context.SaveChanges();
        return metodoPago;
    }

    public bool Actualizar(MetodoPago metodoPago)
    {
        try
        {
            var existente = _context.MetodosPago.Find(metodoPago.Id);
            if (existente == null)
                return false;

            existente.NombreBanco = metodoPago.NombreBanco;
            existente.Icono = metodoPago.Icono;
            existente.TipoCuenta = metodoPago.TipoCuenta;
            existente.Moneda = metodoPago.Moneda;
            existente.NumeroCuenta = metodoPago.NumeroCuenta;
            existente.Mensaje = metodoPago.Mensaje;
            existente.Orden = metodoPago.Orden;
            existente.Activo = metodoPago.Activo;
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
            var metodoPago = _context.MetodosPago.Find(id);
            if (metodoPago == null)
                return false;

            _context.MetodosPago.Remove(metodoPago);
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
                var metodoPago = _context.MetodosPago.Find(kvp.Key);
                if (metodoPago != null)
                {
                    metodoPago.Orden = kvp.Value;
                    metodoPago.FechaActualizacion = DateTime.UtcNow;
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


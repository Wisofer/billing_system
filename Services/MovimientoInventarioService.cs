using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class MovimientoInventarioService : IMovimientoInventarioService
{
    private readonly ApplicationDbContext _context;
    private readonly IEquipoService _equipoService;

    public MovimientoInventarioService(ApplicationDbContext context, IEquipoService equipoService)
    {
        _context = context;
        _equipoService = equipoService;
    }

    public List<MovimientoInventario> ObtenerTodos()
    {
        return _context.MovimientosInventario
            .Include(m => m.Usuario)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.Equipo)
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }

    public MovimientoInventario? ObtenerPorId(int id)
    {
        return _context.MovimientosInventario
            .Include(m => m.Usuario)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.Equipo)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.UbicacionOrigen)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.UbicacionDestino)
            .FirstOrDefault(m => m.Id == id);
    }

    public List<MovimientoInventario> ObtenerPorEquipo(int equipoId)
    {
        return _context.MovimientosInventario
            .Include(m => m.Usuario)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.Equipo)
            .Where(m => m.EquipoMovimientos.Any(em => em.EquipoId == equipoId))
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }

    public List<MovimientoInventario> ObtenerPorFecha(DateTime fechaInicio, DateTime fechaFin)
    {
        return _context.MovimientosInventario
            .Include(m => m.Usuario)
            .Include(m => m.EquipoMovimientos)
                .ThenInclude(em => em.Equipo)
            .Where(m => m.Fecha >= fechaInicio && m.Fecha <= fechaFin)
            .OrderByDescending(m => m.Fecha)
            .ToList();
    }

    public MovimientoInventario Crear(MovimientoInventario movimiento, List<EquipoMovimiento> equipoMovimientos)
    {
        movimiento.Fecha = DateTime.Now;
        movimiento.FechaCreacion = DateTime.Now;

        _context.MovimientosInventario.Add(movimiento);
        _context.SaveChanges();

        // Agregar equipos al movimiento
        foreach (var equipoMov in equipoMovimientos)
        {
            equipoMov.MovimientoInventarioId = movimiento.Id;
            _context.EquipoMovimientos.Add(equipoMov);

            // Actualizar stock del equipo
            var equipo = _equipoService.ObtenerPorId(equipoMov.EquipoId);
            if (equipo != null)
            {
                if (movimiento.Tipo == SD.TipoMovimientoEntrada)
                {
                    equipo.Stock += equipoMov.Cantidad;
                }
                else if (movimiento.Tipo == SD.TipoMovimientoSalida)
                {
                    equipo.Stock -= equipoMov.Cantidad;
                    if (equipo.Stock < 0) equipo.Stock = 0; // No permitir stock negativo
                }

                equipo.FechaActualizacion = DateTime.Now;
            }
        }

        _context.SaveChanges();
        return movimiento;
    }

    public bool Eliminar(int id)
    {
        var movimiento = ObtenerPorId(id);
        if (movimiento == null)
            return false;

        // Revertir cambios en stock
        foreach (var equipoMov in movimiento.EquipoMovimientos)
        {
            var equipo = _equipoService.ObtenerPorId(equipoMov.EquipoId);
            if (equipo != null)
            {
                if (movimiento.Tipo == SD.TipoMovimientoEntrada)
                {
                    equipo.Stock -= equipoMov.Cantidad;
                    if (equipo.Stock < 0) equipo.Stock = 0;
                }
                else if (movimiento.Tipo == SD.TipoMovimientoSalida)
                {
                    equipo.Stock += equipoMov.Cantidad;
                }

                equipo.FechaActualizacion = DateTime.Now;
            }
        }

        _context.MovimientosInventario.Remove(movimiento);
        _context.SaveChanges();
        return true;
    }
}


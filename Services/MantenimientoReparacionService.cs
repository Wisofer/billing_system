using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class MantenimientoReparacionService : IMantenimientoReparacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEquipoService _equipoService;

    public MantenimientoReparacionService(ApplicationDbContext context, IEquipoService equipoService)
    {
        _context = context;
        _equipoService = equipoService;
    }

    public List<MantenimientoReparacion> ObtenerTodos()
    {
        return _context.MantenimientosReparaciones
            .Include(m => m.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .OrderByDescending(m => m.FechaCreacion)
            .ToList();
    }

    public MantenimientoReparacion? ObtenerPorId(int id)
    {
        return _context.MantenimientosReparaciones
            .Include(m => m.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .FirstOrDefault(m => m.Id == id);
    }

    public List<MantenimientoReparacion> ObtenerPorEquipo(int equipoId)
    {
        return _context.MantenimientosReparaciones
            .Include(m => m.Equipo)
            .Where(m => m.EquipoId == equipoId)
            .OrderByDescending(m => m.FechaCreacion)
            .ToList();
    }

    public List<MantenimientoReparacion> ObtenerPorEstado(string estado)
    {
        return _context.MantenimientosReparaciones
            .Include(m => m.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Where(m => m.Estado == estado)
            .OrderByDescending(m => m.FechaCreacion)
            .ToList();
    }

    public MantenimientoReparacion Crear(MantenimientoReparacion mantenimiento)
    {
        var equipo = _equipoService.ObtenerPorId(mantenimiento.EquipoId);
        if (equipo == null)
            throw new Exception("Equipo no encontrado");

        mantenimiento.FechaCreacion = DateTime.Now;
        mantenimiento.Estado = mantenimiento.Estado ?? SD.EstadoMantenimientoProgramado;

        _context.MantenimientosReparaciones.Add(mantenimiento);

        // Si es correctivo y el equipo está disponible, cambiar estado a "En reparación"
        if (mantenimiento.Tipo == SD.TipoMantenimientoCorrectivo && 
            mantenimiento.Estado == SD.EstadoMantenimientoEnProceso &&
            equipo.Estado == SD.EstadoEquipoDisponible)
        {
            _equipoService.CambiarEstado(equipo.Id, SD.EstadoEquipoEnReparacion, 0, "Mantenimiento correctivo iniciado");
        }

        _context.SaveChanges();
        return mantenimiento;
    }

    public MantenimientoReparacion Actualizar(MantenimientoReparacion mantenimiento)
    {
        var existente = ObtenerPorId(mantenimiento.Id);
        if (existente == null)
            throw new Exception("Mantenimiento no encontrado");

        var estadoAnterior = existente.Estado;
        var equipo = _equipoService.ObtenerPorId(mantenimiento.EquipoId);

        existente.Tipo = mantenimiento.Tipo;
        existente.FechaProgramada = mantenimiento.FechaProgramada;
        existente.FechaInicio = mantenimiento.FechaInicio;
        existente.FechaFin = mantenimiento.FechaFin;
        existente.ProveedorTecnico = mantenimiento.ProveedorTecnico;
        existente.Costo = mantenimiento.Costo;
        existente.ProblemaReportado = mantenimiento.ProblemaReportado;
        existente.SolucionAplicada = mantenimiento.SolucionAplicada;
        existente.Estado = mantenimiento.Estado;
        existente.Observaciones = mantenimiento.Observaciones;

        // Si se completó el mantenimiento y el equipo estaba en reparación, cambiar a Disponible
        if (estadoAnterior != SD.EstadoMantenimientoCompletado && 
            mantenimiento.Estado == SD.EstadoMantenimientoCompletado &&
            equipo != null && equipo.Estado == SD.EstadoEquipoEnReparacion)
        {
            _equipoService.CambiarEstado(equipo.Id, SD.EstadoEquipoDisponible, 0, "Mantenimiento completado");
        }

        // Si se inició el mantenimiento correctivo
        if (mantenimiento.Tipo == SD.TipoMantenimientoCorrectivo && 
            mantenimiento.Estado == SD.EstadoMantenimientoEnProceso &&
            equipo != null && equipo.Estado == SD.EstadoEquipoDisponible)
        {
            _equipoService.CambiarEstado(equipo.Id, SD.EstadoEquipoEnReparacion, 0, "Mantenimiento correctivo iniciado");
        }

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var mantenimiento = ObtenerPorId(id);
        if (mantenimiento == null)
            return false;

        _context.MantenimientosReparaciones.Remove(mantenimiento);
        _context.SaveChanges();
        return true;
    }
}


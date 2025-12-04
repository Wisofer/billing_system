using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class AsignacionEquipoService : IAsignacionEquipoService
{
    private readonly ApplicationDbContext _context;
    private readonly IEquipoService _equipoService;

    public AsignacionEquipoService(ApplicationDbContext context, IEquipoService equipoService)
    {
        _context = context;
        _equipoService = equipoService;
    }

    public List<AsignacionEquipo> ObtenerTodas()
    {
        return _context.AsignacionesEquipo
            .Include(a => a.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Include(a => a.Cliente)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToList();
    }

    public List<AsignacionEquipo> ObtenerActivas()
    {
        return _context.AsignacionesEquipo
            .Include(a => a.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Include(a => a.Cliente)
            .Where(a => a.Estado == SD.EstadoAsignacionActiva)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToList();
    }

    public AsignacionEquipo? ObtenerPorId(int id)
    {
        return _context.AsignacionesEquipo
            .Include(a => a.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Include(a => a.Cliente)
            .FirstOrDefault(a => a.Id == id);
    }

    public List<AsignacionEquipo> ObtenerPorEquipo(int equipoId)
    {
        return _context.AsignacionesEquipo
            .Include(a => a.Equipo)
            .Include(a => a.Cliente)
            .Where(a => a.EquipoId == equipoId)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToList();
    }

    public List<AsignacionEquipo> ObtenerPorCliente(int clienteId)
    {
        return _context.AsignacionesEquipo
            .Include(a => a.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Where(a => a.ClienteId == clienteId)
            .OrderByDescending(a => a.FechaAsignacion)
            .ToList();
    }

    public AsignacionEquipo Crear(AsignacionEquipo asignacion)
    {
        var equipo = _equipoService.ObtenerPorId(asignacion.EquipoId);
        if (equipo == null)
            throw new Exception("Equipo no encontrado");

        // Validar stock disponible
        if (equipo.Stock < asignacion.Cantidad)
        {
            throw new Exception($"No hay suficiente stock disponible. Stock actual: {equipo.Stock}");
        }

        asignacion.FechaAsignacion = DateTime.Now;
        asignacion.Estado = SD.EstadoAsignacionActiva;
        asignacion.FechaCreacion = DateTime.Now;

        _context.AsignacionesEquipo.Add(asignacion);

        // Actualizar estado del equipo si es necesario
        if (equipo.Estado == SD.EstadoEquipoDisponible)
        {
            _equipoService.CambiarEstado(equipo.Id, SD.EstadoEquipoEnUso, 0, "Asignación de equipo");
        }

        // Reducir stock
        equipo.Stock -= asignacion.Cantidad;
        equipo.FechaActualizacion = DateTime.Now;

        _context.SaveChanges();
        return asignacion;
    }

    public AsignacionEquipo Actualizar(AsignacionEquipo asignacion)
    {
        var existente = ObtenerPorId(asignacion.Id);
        if (existente == null)
            throw new Exception("Asignación no encontrada");

        existente.Cantidad = asignacion.Cantidad;
        existente.ClienteId = asignacion.ClienteId;
        existente.EmpleadoNombre = asignacion.EmpleadoNombre;
        existente.FechaDevolucionEsperada = asignacion.FechaDevolucionEsperada;
        existente.Observaciones = asignacion.Observaciones;

        _context.SaveChanges();
        return existente;
    }

    public bool Devolver(int asignacionId, DateTime fechaDevolucion)
    {
        var asignacion = ObtenerPorId(asignacionId);
        if (asignacion == null)
            return false;

        if (asignacion.Estado != SD.EstadoAsignacionActiva)
            return false; // Ya fue devuelta

        asignacion.Estado = SD.EstadoAsignacionDevuelta;
        asignacion.FechaDevolucionReal = fechaDevolucion;

        // Devolver stock
        var equipo = _equipoService.ObtenerPorId(asignacion.EquipoId);
        if (equipo != null)
        {
            equipo.Stock += asignacion.Cantidad;
            equipo.FechaActualizacion = DateTime.Now;

            // Si no hay más asignaciones activas, cambiar estado a Disponible
            var tieneOtrasAsignacionesActivas = _context.AsignacionesEquipo
                .Any(a => a.EquipoId == equipo.Id && a.Id != asignacionId && a.Estado == SD.EstadoAsignacionActiva);

            if (!tieneOtrasAsignacionesActivas && equipo.Estado == SD.EstadoEquipoEnUso)
            {
                _equipoService.CambiarEstado(equipo.Id, SD.EstadoEquipoDisponible, 0, "Devolución de equipo");
            }
        }

        _context.SaveChanges();
        return true;
    }

    public bool Eliminar(int id)
    {
        var asignacion = ObtenerPorId(id);
        if (asignacion == null)
            return false;

        // Si está activa, devolver stock antes de eliminar
        if (asignacion.Estado == SD.EstadoAsignacionActiva)
        {
            var equipo = _equipoService.ObtenerPorId(asignacion.EquipoId);
            if (equipo != null)
            {
                equipo.Stock += asignacion.Cantidad;
                equipo.FechaActualizacion = DateTime.Now;
            }
        }

        _context.AsignacionesEquipo.Remove(asignacion);
        _context.SaveChanges();
        return true;
    }
}


using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class EgresoService : IEgresoService
{
    private readonly ApplicationDbContext _context;

    public EgresoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Egreso> ObtenerTodos()
    {
        return _context.Egresos
            .Include(e => e.Usuario)
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .ToList();
    }

    public List<Egreso> ObtenerActivos()
    {
        return _context.Egresos
            .Include(e => e.Usuario)
            .Where(e => e.Activo)
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .ToList();
    }

    public Egreso? ObtenerPorId(int id)
    {
        return _context.Egresos
            .Include(e => e.Usuario)
            .FirstOrDefault(e => e.Id == id);
    }

    public Egreso? ObtenerPorCodigo(string codigo)
    {
        return _context.Egresos
            .Include(e => e.Usuario)
            .FirstOrDefault(e => e.Codigo == codigo);
    }

    public PagedResult<Egreso> ObtenerPaginados(int pagina, int tamanoPagina, string? busqueda = null, string? categoria = null, DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        var query = _context.Egresos
            .Include(e => e.Usuario)
            .Where(e => e.Activo)
            .AsQueryable();

        // Filtrar por búsqueda
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var busquedaNormalizada = Helpers.NormalizarTexto(busqueda);
            // Cargar en memoria para aplicar normalización
            var todosEgresos = query.ToList();
            var egresosFiltrados = todosEgresos.Where(e =>
                Helpers.NormalizarTexto(e.Codigo).Contains(busquedaNormalizada) ||
                Helpers.NormalizarTexto(e.Descripcion).Contains(busquedaNormalizada) ||
                Helpers.NormalizarTexto(e.Proveedor).Contains(busquedaNormalizada) ||
                Helpers.NormalizarTexto(e.NumeroFactura).Contains(busquedaNormalizada)
            ).ToList();

            // Filtrar por categoría
            if (!string.IsNullOrWhiteSpace(categoria))
            {
                egresosFiltrados = egresosFiltrados.Where(e => e.Categoria == categoria).ToList();
            }

            // Filtrar por fechas
            if (fechaInicio.HasValue)
            {
                egresosFiltrados = egresosFiltrados.Where(e => e.Fecha.Date >= fechaInicio.Value.Date).ToList();
            }
            if (fechaFin.HasValue)
            {
                egresosFiltrados = egresosFiltrados.Where(e => e.Fecha.Date <= fechaFin.Value.Date).ToList();
            }

            var totalFiltrado = egresosFiltrados.Count;
            var itemsFiltrados = egresosFiltrados
                .OrderByDescending(e => e.Fecha)
                .ThenByDescending(e => e.Id)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToList();

            return new PagedResult<Egreso>
            {
                Items = itemsFiltrados,
                TotalItems = totalFiltrado,
                CurrentPage = pagina,
                PageSize = tamanoPagina
            };
        }

        // Filtrar por categoría
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            query = query.Where(e => e.Categoria == categoria);
        }

        // Filtrar por fechas
        if (fechaInicio.HasValue)
        {
            query = query.Where(e => e.Fecha.Date >= fechaInicio.Value.Date);
        }
        if (fechaFin.HasValue)
        {
            query = query.Where(e => e.Fecha.Date <= fechaFin.Value.Date);
        }

        var total = query.Count();
        var items = query
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return new PagedResult<Egreso>
        {
            Items = items,
            TotalItems = total,
            CurrentPage = pagina,
            PageSize = tamanoPagina
        };
    }

    public void Crear(Egreso egreso)
    {
        if (string.IsNullOrWhiteSpace(egreso.Codigo))
        {
            egreso.Codigo = GenerarCodigo();
        }
        egreso.FechaCreacion = DateTime.Now;
        egreso.Activo = true;
        _context.Egresos.Add(egreso);
        _context.SaveChanges();
    }

    public void Actualizar(Egreso egreso)
    {
        var existente = _context.Egresos.Find(egreso.Id);
        if (existente == null) return;

        existente.Descripcion = egreso.Descripcion;
        existente.Categoria = egreso.Categoria;
        existente.Monto = egreso.Monto;
        existente.Fecha = egreso.Fecha;
        existente.NumeroFactura = egreso.NumeroFactura;
        existente.Proveedor = egreso.Proveedor;
        existente.MetodoPago = egreso.MetodoPago;
        existente.Observaciones = egreso.Observaciones;
        existente.FechaActualizacion = DateTime.Now;

        _context.SaveChanges();
    }

    public void Eliminar(int id)
    {
        var egreso = _context.Egresos.Find(id);
        if (egreso == null) return;

        // Soft delete
        egreso.Activo = false;
        egreso.FechaActualizacion = DateTime.Now;
        _context.SaveChanges();
    }

    public string GenerarCodigo()
    {
        var ultimoEgreso = _context.Egresos
            .OrderByDescending(e => e.Id)
            .FirstOrDefault();

        int numero = 1;
        if (ultimoEgreso != null && !string.IsNullOrEmpty(ultimoEgreso.Codigo))
        {
            var partes = ultimoEgreso.Codigo.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out int ultimoNumero))
            {
                numero = ultimoNumero + 1;
            }
        }

        return $"EGR-{numero:D4}";
    }

    public decimal CalcularTotalEgresos()
    {
        return _context.Egresos
            .Where(e => e.Activo)
            .Sum(e => e.Monto);
    }

    public decimal CalcularTotalEgresosPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
    {
        return _context.Egresos
            .Where(e => e.Activo && e.Fecha.Date >= fechaInicio.Date && e.Fecha.Date <= fechaFin.Date)
            .Sum(e => e.Monto);
    }

    public decimal CalcularTotalEgresosMesActual()
    {
        var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);
        return CalcularTotalEgresosPorPeriodo(inicioMes, finMes);
    }

    public Dictionary<string, decimal> ObtenerEgresosPorCategoria()
    {
        return _context.Egresos
            .Where(e => e.Activo)
            .GroupBy(e => e.Categoria)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Monto));
    }

    public Dictionary<string, decimal> ObtenerEgresosPorCategoriaPeriodo(DateTime fechaInicio, DateTime fechaFin)
    {
        return _context.Egresos
            .Where(e => e.Activo && e.Fecha.Date >= fechaInicio.Date && e.Fecha.Date <= fechaFin.Date)
            .GroupBy(e => e.Categoria)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Monto));
    }

    public List<Egreso> ObtenerUltimosEgresos(int cantidad)
    {
        return _context.Egresos
            .Include(e => e.Usuario)
            .Where(e => e.Activo)
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .Take(cantidad)
            .ToList();
    }
}


using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class EquipoService : IEquipoService
{
    private readonly ApplicationDbContext _context;

    public EquipoService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Equipo> ObtenerTodos()
    {
        return _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .Include(e => e.Proveedor)
            .Where(e => e.Activo)
            .OrderByDescending(e => e.FechaCreacion)
            .ToList();
    }

    public PagedResult<Equipo> ObtenerPaginados(int pagina = 1, int tamanoPagina = 10, string? busqueda = null, string? estado = null, int? categoriaId = null, int? ubicacionId = null, bool? soloActivos = true)
    {
        var query = _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .Include(e => e.Proveedor)
            .Include(e => e.AsignacionesEquipo.Where(a => a.Estado == "Activa"))
                .ThenInclude(a => a.Cliente)
            .AsQueryable();
        
        // Filtro por estado del sistema (activo/inactivo)
        if (soloActivos.HasValue)
        {
            query = query.Where(e => e.Activo == soloActivos.Value);
        }
        // Si soloActivos es null, mostrar todos (activos + inactivos)

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.ToLower();
            query = query.Where(e =>
                e.Nombre.ToLower().Contains(termino) ||
                e.Codigo.ToLower().Contains(termino) ||
                (e.NumeroSerie != null && e.NumeroSerie.ToLower().Contains(termino)) ||
                (e.Marca != null && e.Marca.ToLower().Contains(termino)) ||
                (e.Modelo != null && e.Modelo.ToLower().Contains(termino)));
        }

        if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
        {
            query = query.Where(e => e.Estado == estado);
        }

        if (categoriaId.HasValue && categoriaId.Value > 0)
        {
            query = query.Where(e => e.CategoriaEquipoId == categoriaId.Value);
        }

        if (ubicacionId.HasValue && ubicacionId.Value > 0)
        {
            query = query.Where(e => e.UbicacionId == ubicacionId.Value);
        }

        var totalItems = query.Count();

        var items = query
            .OrderByDescending(e => e.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return new PagedResult<Equipo>
        {
            Items = items,
            CurrentPage = pagina,
            PageSize = tamanoPagina,
            TotalItems = totalItems
        };
    }

    public Equipo? ObtenerPorId(int id)
    {
        return _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .Include(e => e.Proveedor)
            .Include(e => e.MovimientosInventario)
                .ThenInclude(m => m.Usuario)
            .Include(e => e.AsignacionesEquipo)
                .ThenInclude(a => a.Cliente)
            .Include(e => e.MantenimientosReparaciones)
            .Include(e => e.HistorialEstados)
                .ThenInclude(h => h.Usuario)
            .FirstOrDefault(e => e.Id == id); // Removido filtro e.Activo para poder obtener equipos deshabilitados también
    }

    public Equipo? ObtenerPorCodigo(string codigo)
    {
        return _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .FirstOrDefault(e => e.Codigo == codigo && e.Activo);
    }

    public Equipo? ObtenerPorNumeroSerie(string numeroSerie)
    {
        return _context.Equipos
            .FirstOrDefault(e => e.NumeroSerie == numeroSerie && e.Activo);
    }

    public List<Equipo> Buscar(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return ObtenerTodos();

        termino = termino.ToLower();
        return _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .Where(e => e.Activo && (
                e.Nombre.ToLower().Contains(termino) ||
                e.Codigo.ToLower().Contains(termino) ||
                (e.NumeroSerie != null && e.NumeroSerie.ToLower().Contains(termino)) ||
                (e.Marca != null && e.Marca.ToLower().Contains(termino)) ||
                (e.Modelo != null && e.Modelo.ToLower().Contains(termino))
            ))
            .OrderByDescending(e => e.FechaCreacion)
            .ToList();
    }

    public Equipo Crear(Equipo equipo)
    {
        // Validar duplicados
        if (ExisteCodigo(equipo.Codigo))
        {
            throw new Exception($"Ya existe un equipo con el código '{equipo.Codigo}'");
        }

        if (!string.IsNullOrWhiteSpace(equipo.NumeroSerie) && ExisteNumeroSerie(equipo.NumeroSerie))
        {
            throw new Exception($"Ya existe un equipo con el número de serie '{equipo.NumeroSerie}'");
        }

        // Generar código automáticamente si no viene
        if (string.IsNullOrWhiteSpace(equipo.Codigo))
        {
            equipo.Codigo = GenerarCodigoEquipoUnico();
        }

        equipo.FechaCreacion = DateTime.Now;
        equipo.Activo = true;
        equipo.Estado = equipo.Estado ?? SD.EstadoEquipoDisponible;

        _context.Equipos.Add(equipo);
        _context.SaveChanges();

        // Registrar en historial
        RegistrarCambioEstado(equipo.Id, "", equipo.Estado, 0, "Creación de equipo");

        return equipo;
    }

    public Equipo Actualizar(Equipo equipo)
    {
        var existente = ObtenerPorId(equipo.Id);
        if (existente == null)
            throw new Exception("Equipo no encontrado");

        // Validar duplicados
        if (ExisteCodigo(equipo.Codigo, equipo.Id))
        {
            throw new Exception($"Ya existe otro equipo con el código '{equipo.Codigo}'");
        }

        if (!string.IsNullOrWhiteSpace(equipo.NumeroSerie) && ExisteNumeroSerie(equipo.NumeroSerie, equipo.Id))
        {
            throw new Exception($"Ya existe otro equipo con el número de serie '{equipo.NumeroSerie}'");
        }

        var estadoAnterior = existente.Estado;

        // Actualizar campos
        existente.Codigo = equipo.Codigo;
        existente.Nombre = equipo.Nombre;
        existente.Descripcion = equipo.Descripcion;
        existente.NumeroSerie = equipo.NumeroSerie;
        existente.Marca = equipo.Marca;
        existente.Modelo = equipo.Modelo;
        existente.CategoriaEquipoId = equipo.CategoriaEquipoId;
        existente.UbicacionId = equipo.UbicacionId;
        existente.Stock = equipo.Stock;
        existente.StockMinimo = equipo.StockMinimo;
        existente.PrecioCompra = equipo.PrecioCompra;
        existente.FechaAdquisicion = equipo.FechaAdquisicion;
        existente.ProveedorId = equipo.ProveedorId;
        existente.Observaciones = equipo.Observaciones;
        existente.FechaActualizacion = DateTime.Now;

        // Si cambió el estado, registrar en historial
        if (existente.Estado != equipo.Estado)
        {
            existente.Estado = equipo.Estado;
            RegistrarCambioEstado(existente.Id, estadoAnterior, equipo.Estado, 0, "Actualización manual");
        }

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var equipo = ObtenerPorId(id);
        if (equipo == null)
            return false;

        // ELIMINACIÓN EN CASCADA:
        // 1. Eliminar todas las asignaciones (activas e inactivas)
        var asignaciones = _context.AsignacionesEquipo
            .Where(a => a.EquipoId == id)
            .ToList();
        
        if (asignaciones.Any())
        {
            _context.AsignacionesEquipo.RemoveRange(asignaciones);
        }

        // 2. Eliminar historial de estados
        var historialEstados = _context.HistorialEstadosEquipo
            .Where(h => h.EquipoId == id)
            .ToList();
        
        if (historialEstados.Any())
        {
            _context.HistorialEstadosEquipo.RemoveRange(historialEstados);
        }

        // 3. Eliminar movimientos de inventario relacionados (EquipoMovimiento)
        var equipoMovimientos = _context.EquipoMovimientos
            .Where(em => em.EquipoId == id)
            .ToList();
        
        if (equipoMovimientos.Any())
        {
            _context.EquipoMovimientos.RemoveRange(equipoMovimientos);
        }

        // 4. Eliminar mantenimientos/reparaciones
        var mantenimientos = _context.MantenimientosReparaciones
            .Where(m => m.EquipoId == id)
            .ToList();
        
        if (mantenimientos.Any())
        {
            _context.MantenimientosReparaciones.RemoveRange(mantenimientos);
        }

        // 5. Finalmente, eliminar el equipo FÍSICAMENTE (hard delete)
        _context.Equipos.Remove(equipo);
        
        _context.SaveChanges();
        return true;
    }

    public bool CambiarEstado(int equipoId, string nuevoEstado, int usuarioId, string? motivo = null)
    {
        var equipo = ObtenerPorId(equipoId);
        if (equipo == null)
            return false;

        var estadoAnterior = equipo.Estado;
        equipo.Estado = nuevoEstado;
        equipo.FechaActualizacion = DateTime.Now;

        // Registrar en historial
        RegistrarCambioEstado(equipoId, estadoAnterior, nuevoEstado, usuarioId, motivo);

        _context.SaveChanges();
        return true;
    }

    private void RegistrarCambioEstado(int equipoId, string estadoAnterior, string estadoNuevo, int usuarioId, string? motivo)
    {
        // Solo registrar en historial si hay un usuario válido
        if (usuarioId > 0)
        {
            var historial = new HistorialEstadoEquipo
            {
                EquipoId = equipoId,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevo,
                UsuarioId = usuarioId,
                Motivo = motivo,
                FechaCambio = DateTime.Now
            };

            _context.HistorialEstadosEquipo.Add(historial);
        }
        // Si no hay usuario válido (cambios automáticos del sistema), no registrar en historial
    }

    public bool ExisteCodigo(string codigo, int? idExcluir = null)
    {
        return _context.Equipos
            .Any(e => e.Codigo == codigo && e.Activo && (idExcluir == null || e.Id != idExcluir));
    }

    public bool ExisteNumeroSerie(string numeroSerie, int? idExcluir = null)
    {
        if (string.IsNullOrWhiteSpace(numeroSerie))
            return false;

        return _context.Equipos
            .Any(e => e.NumeroSerie != null && 
                     e.NumeroSerie.Trim().ToUpper() == numeroSerie.Trim().ToUpper() && 
                     e.Activo && 
                     (idExcluir == null || e.Id != idExcluir));
    }

    public int ObtenerTotal()
    {
        return _context.Equipos.Count(e => e.Activo);
    }

    public int ObtenerTotalPorEstado(string estado)
    {
        return _context.Equipos.Count(e => e.Activo && e.Estado == estado);
    }

    public List<Equipo> ObtenerConStockMinimo()
    {
        return _context.Equipos
            .Include(e => e.CategoriaEquipo)
            .Include(e => e.Ubicacion)
            .Where(e => e.Activo && e.StockMinimo > 0 && e.Stock <= e.StockMinimo)
            .OrderBy(e => e.Stock)
            .ToList();
    }

    public decimal ObtenerValorTotalInventario()
    {
        return _context.Equipos
            .Where(e => e.Activo && e.PrecioCompra.HasValue)
            .Sum(e => (e.PrecioCompra!.Value * e.Stock));
    }

    private string GenerarCodigoEquipoUnico()
    {
        // Genera códigos para equipos con el mismo formato que clientes: EMS_XXXXXX (6 dígitos),
        // pero con lógica independiente para evitar cualquier conflicto raro de metadata.
        string codigo;
        int intentos = 0;
        const int maxIntentos = 100;

        do
        {
            var numeros = Random.Shared.Next(100000, 1000000); // 6 dígitos
            codigo = $"EMS_{numeros}";
            intentos++;

            if (intentos >= maxIntentos)
            {
                var timestamp = DateTime.Now.Ticks % 1_000_000;
                codigo = $"EMS_{timestamp:D6}";
                break;
            }
        } while (ExisteCodigo(codigo));

        return codigo;
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using billing_system.Data;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Controllers.Web;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class MovimientosInventarioController : Controller
{
    private readonly IMovimientoInventarioService _movimientoService;
    private readonly IEquipoService _equipoService;
    private readonly IUbicacionService _ubicacionService;
    private readonly ApplicationDbContext _context;

    public MovimientosInventarioController(
        IMovimientoInventarioService movimientoService,
        IEquipoService equipoService,
        IUbicacionService ubicacionService,
        ApplicationDbContext context)
    {
        _movimientoService = movimientoService;
        _equipoService = equipoService;
        _ubicacionService = ubicacionService;
        _context = context;
    }

    [HttpGet("/movimientos-inventario")]
    public IActionResult Index(string? tipo = null, string? subtipo = null, int? equipoId = null, string? fechaInicio = null, string? fechaFin = null, int pagina = 1, int tamanoPagina = 20)
    {
        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;

        // Obtener todos los movimientos
        var todosMovimientos = _movimientoService.ObtenerTodos();

        // Aplicar filtros
        if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
        {
            todosMovimientos = todosMovimientos.Where(m => m.Tipo == tipo).ToList();
        }

        if (!string.IsNullOrWhiteSpace(subtipo) && subtipo != "Todos")
        {
            todosMovimientos = todosMovimientos.Where(m => m.Subtipo == subtipo).ToList();
        }

        if (equipoId.HasValue && equipoId.Value > 0)
        {
            todosMovimientos = todosMovimientos.Where(m => 
                m.EquipoMovimientos.Any(em => em.EquipoId == equipoId.Value)).ToList();
        }

        DateTime? fechaInicioDate = null;
        DateTime? fechaFinDate = null;

        if (!string.IsNullOrWhiteSpace(fechaInicio) && DateTime.TryParse(fechaInicio, out var fi))
        {
            fechaInicioDate = fi;
        }

        if (!string.IsNullOrWhiteSpace(fechaFin) && DateTime.TryParse(fechaFin, out var ff))
        {
            fechaFinDate = ff;
        }

        if (fechaInicioDate.HasValue)
        {
            todosMovimientos = todosMovimientos.Where(m => m.Fecha >= fechaInicioDate.Value).ToList();
        }

        if (fechaFinDate.HasValue)
        {
            todosMovimientos = todosMovimientos.Where(m => m.Fecha <= fechaFinDate.Value.AddDays(1).AddSeconds(-1)).ToList();
        }

        // Paginación
        var totalItems = todosMovimientos.Count;
        var items = todosMovimientos
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        var resultado = new billing_system.Models.ViewModels.PagedResult<MovimientoInventario>
        {
            Items = items,
            CurrentPage = pagina,
            PageSize = tamanoPagina,
            TotalItems = totalItems
        };

        // Estadísticas
        var totalEntradas = _movimientoService.ObtenerTodos().Count(m => m.Tipo == SD.TipoMovimientoEntrada);
        var totalSalidas = _movimientoService.ObtenerTodos().Count(m => m.Tipo == SD.TipoMovimientoSalida);
        var movimientosHoy = _movimientoService.ObtenerTodos().Count(m => m.Fecha.Date == DateTime.Now.Date);
        var movimientosMes = _movimientoService.ObtenerTodos().Count(m => 
            m.Fecha.Year == DateTime.Now.Year && m.Fecha.Month == DateTime.Now.Month);

        ViewBag.Tipo = tipo;
        ViewBag.Subtipo = subtipo;
        ViewBag.EquipoId = equipoId;
        ViewBag.FechaInicio = fechaInicio;
        ViewBag.FechaFin = fechaFin;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = totalItems;
        ViewBag.TotalEntradas = totalEntradas;
        ViewBag.TotalSalidas = totalSalidas;
        ViewBag.MovimientosHoy = movimientosHoy;
        ViewBag.MovimientosMes = movimientosMes;

        // Listas para filtros
        ViewBag.Tipos = new[] { "Todos", SD.TipoMovimientoEntrada, SD.TipoMovimientoSalida };
        ViewBag.Subtipos = new[] { 
            "Todos", 
            SD.SubtipoMovimientoCompra, 
            SD.SubtipoMovimientoVenta, 
            SD.SubtipoMovimientoAsignacion, 
            SD.SubtipoMovimientoDevolucion, 
            SD.SubtipoMovimientoAjuste, 
            SD.SubtipoMovimientoDano, 
            SD.SubtipoMovimientoTransferencia 
        };
        ViewBag.Equipos = _equipoService.ObtenerTodos().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();

        return View(resultado);
    }

    [HttpGet("/movimientos-inventario/crear")]
    public IActionResult Crear()
    {
        ViewBag.Equipos = _equipoService.ObtenerTodos().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Tipos = new[] { SD.TipoMovimientoEntrada, SD.TipoMovimientoSalida };
        ViewBag.SubtiposEntrada = new[] { 
            SD.SubtipoMovimientoCompra, 
            SD.SubtipoMovimientoDevolucion, 
            SD.SubtipoMovimientoAjuste 
        };
        ViewBag.SubtiposSalida = new[] { 
            SD.SubtipoMovimientoVenta, 
            SD.SubtipoMovimientoAsignacion, 
            SD.SubtipoMovimientoDano 
        };
        ViewBag.SubtiposTransferencia = new[] { SD.SubtipoMovimientoTransferencia };

        return View();
    }

    [HttpPost("/movimientos-inventario/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(MovimientoInventario movimiento, List<int> EquipoIds, List<int> Cantidades, List<decimal?> PreciosUnitarios, int? UbicacionOrigenId, int? UbicacionDestinoId)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(movimiento.Tipo))
        {
            ModelState.AddModelError("Tipo", "El tipo de movimiento es requerido");
        }

        if (string.IsNullOrWhiteSpace(movimiento.Subtipo))
        {
            ModelState.AddModelError("Subtipo", "El subtipo de movimiento es requerido");
        }

        if (EquipoIds == null || !EquipoIds.Any())
        {
            ModelState.AddModelError("", "Debe seleccionar al menos un equipo");
        }

        if (EquipoIds != null && Cantidades != null)
        {
            for (int i = 0; i < EquipoIds.Count; i++)
            {
                if (Cantidades[i] <= 0)
                {
                    ModelState.AddModelError("", $"La cantidad del equipo {i + 1} debe ser mayor a 0");
                }

                // Validar stock disponible para salidas
                if (movimiento.Tipo == SD.TipoMovimientoSalida)
                {
                    var equipo = _equipoService.ObtenerPorId(EquipoIds[i]);
                    if (equipo != null && equipo.Stock < Cantidades[i])
                    {
                        ModelState.AddModelError("", $"No hay suficiente stock para {equipo.Nombre}. Stock disponible: {equipo.Stock}");
                    }
                }
            }
        }

        // Validar transferencias
        if (movimiento.Subtipo == SD.SubtipoMovimientoTransferencia)
        {
            if (!UbicacionOrigenId.HasValue || !UbicacionDestinoId.HasValue)
            {
                ModelState.AddModelError("", "Las transferencias requieren ubicación de origen y destino");
            }
            if (UbicacionOrigenId == UbicacionDestinoId)
            {
                ModelState.AddModelError("", "La ubicación de origen y destino no pueden ser la misma");
            }
        }

        ModelState.Remove("Usuario");
        ModelState.Remove("EquipoMovimientos");

        if (!ModelState.IsValid)
        {
            ViewBag.Equipos = _equipoService.ObtenerTodos().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Tipos = new[] { SD.TipoMovimientoEntrada, SD.TipoMovimientoSalida };
            ViewBag.SubtiposEntrada = new[] { 
                SD.SubtipoMovimientoCompra, 
                SD.SubtipoMovimientoDevolucion, 
                SD.SubtipoMovimientoAjuste 
            };
            ViewBag.SubtiposSalida = new[] { 
                SD.SubtipoMovimientoVenta, 
                SD.SubtipoMovimientoAsignacion, 
                SD.SubtipoMovimientoDano 
            };
            ViewBag.SubtiposTransferencia = new[] { SD.SubtipoMovimientoTransferencia };
            return View(movimiento);
        }

        try
        {
            // Obtener usuario actual
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (int.TryParse(usuarioIdStr, out int usuarioId))
            {
                movimiento.UsuarioId = usuarioId;
            }
            else
            {
                TempData["Error"] = "No se pudo identificar al usuario";
                return RedirectToAction(nameof(Index));
            }

            // Crear lista de EquipoMovimiento
            var equipoMovimientos = new List<EquipoMovimiento>();
            if (EquipoIds != null && Cantidades != null)
            {
                for (int i = 0; i < EquipoIds.Count; i++)
                {
                    var equipoMov = new EquipoMovimiento
                    {
                        EquipoId = EquipoIds[i],
                        Cantidad = Cantidades[i],
                        PrecioUnitario = PreciosUnitarios != null && i < PreciosUnitarios.Count ? PreciosUnitarios[i] : null
                    };

                // Para transferencias, agregar ubicaciones
                if (movimiento.Subtipo == SD.SubtipoMovimientoTransferencia)
                {
                    equipoMov.UbicacionOrigenId = UbicacionOrigenId;
                    equipoMov.UbicacionDestinoId = UbicacionDestinoId;
                }

                    equipoMovimientos.Add(equipoMov);
                }
            }

            // Crear el movimiento
            _movimientoService.Crear(movimiento, equipoMovimientos);

            // Actualizar ubicación de equipos en transferencias
            if (movimiento.Subtipo == SD.SubtipoMovimientoTransferencia && UbicacionDestinoId.HasValue && EquipoIds != null)
            {
                foreach (var equipoId in EquipoIds)
                {
                    var equipo = _equipoService.ObtenerPorId(equipoId);
                    if (equipo != null && UbicacionDestinoId.HasValue)
                    {
                        equipo.UbicacionId = UbicacionDestinoId.Value;
                        equipo.FechaActualizacion = DateTime.Now;
                    }
                }
                _context.SaveChanges();
            }

            TempData["Success"] = "Movimiento de inventario registrado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al crear el movimiento: {ex.Message}";
            ViewBag.Equipos = _equipoService.ObtenerTodos().Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Tipos = new[] { SD.TipoMovimientoEntrada, SD.TipoMovimientoSalida };
            ViewBag.SubtiposEntrada = new[] { 
                SD.SubtipoMovimientoCompra, 
                SD.SubtipoMovimientoDevolucion, 
                SD.SubtipoMovimientoAjuste 
            };
            ViewBag.SubtiposSalida = new[] { 
                SD.SubtipoMovimientoVenta, 
                SD.SubtipoMovimientoAsignacion, 
                SD.SubtipoMovimientoDano 
            };
            ViewBag.SubtiposTransferencia = new[] { SD.SubtipoMovimientoTransferencia };
            return View(movimiento);
        }
    }

    [HttpGet("/movimientos-inventario/ver/{id}")]
    public IActionResult Ver(int id)
    {
        var movimiento = _movimientoService.ObtenerPorId(id);
        if (movimiento == null)
        {
            TempData["Error"] = "Movimiento no encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(movimiento);
    }

    [HttpPost("/movimientos-inventario/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var eliminado = _movimientoService.Eliminar(id);
            if (eliminado)
            {
                TempData["Success"] = "Movimiento eliminado exitosamente. El stock ha sido revertido.";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el movimiento";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar el movimiento: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}


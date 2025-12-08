using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using billing_system.Data;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Controllers;

[Authorize(Policy = "Administrador")]
[Route("[controller]/[action]")]
public class EquiposController : Controller
{
    private readonly IEquipoService _equipoService;
    private readonly ICategoriaEquipoService _categoriaEquipoService;
    private readonly IUbicacionService _ubicacionService;
    private readonly IProveedorService _proveedorService;
    private readonly IClienteService _clienteService;
    private readonly IAsignacionEquipoService _asignacionEquipoService;
    private readonly ApplicationDbContext _context;

    public EquiposController(
        IEquipoService equipoService,
        ICategoriaEquipoService categoriaEquipoService,
        IUbicacionService ubicacionService,
        IProveedorService proveedorService,
        IClienteService clienteService,
        IAsignacionEquipoService asignacionEquipoService,
        ApplicationDbContext context)
    {
        _equipoService = equipoService;
        _categoriaEquipoService = categoriaEquipoService;
        _ubicacionService = ubicacionService;
        _proveedorService = proveedorService;
        _clienteService = clienteService;
        _asignacionEquipoService = asignacionEquipoService;
        _context = context;
    }

    [HttpGet("/equipos")]
    public IActionResult Index(string? busqueda, string? estado, int? categoriaId, int? ubicacionId, string? estadoSistema, int pagina = 1, int tamanoPagina = 25)
    {
        // Validar parámetros de paginación
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 5) tamanoPagina = 5;
        if (tamanoPagina > 50) tamanoPagina = 50;

        // Determinar filtro de activos/inactivos
        // Por defecto: mostrar solo activos si no se especifica nada
        if (string.IsNullOrEmpty(estadoSistema))
        {
            estadoSistema = "activos";
        }
        
        bool? soloActivos = estadoSistema switch
        {
            "activos" => true,
            "deshabilitados" => false,
            "todos" => null, // Mostrar ambos
            _ => true // Por defecto: solo activos
        };

        var resultado = _equipoService.ObtenerPaginados(pagina, tamanoPagina, busqueda, estado, categoriaId, ubicacionId, soloActivos);

        // Estadísticas
        var totalEquipos = _context.Equipos.Count(e => e.Activo); // Solo activos para total
        var totalDeshabilitados = _context.Equipos.Count(e => !e.Activo);
        var disponibles = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoDisponible);
        var enUso = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoEnUso);
        var danados = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoDanado);
        var enReparacion = _equipoService.ObtenerTotalPorEstado(SD.EstadoEquipoEnReparacion);
        var conStockMinimo = _equipoService.ObtenerConStockMinimo().Count;
        var valorTotal = _equipoService.ObtenerValorTotalInventario();

        ViewBag.Busqueda = busqueda;
        ViewBag.Estado = estado;
        ViewBag.EstadoSistema = estadoSistema; // Ya está garantizado que no es null
        ViewBag.CategoriaId = categoriaId;
        ViewBag.UbicacionId = ubicacionId;
        ViewBag.Pagina = pagina;
        ViewBag.TamanoPagina = tamanoPagina;
        ViewBag.TotalItems = resultado.TotalItems;
        ViewBag.TotalEquipos = totalEquipos;
        ViewBag.TotalDeshabilitados = totalDeshabilitados;
        ViewBag.Disponibles = disponibles;
        ViewBag.EnUso = enUso;
        ViewBag.Danados = danados;
        ViewBag.EnReparacion = enReparacion;
        ViewBag.ConStockMinimo = conStockMinimo;
        ViewBag.ValorTotal = valorTotal;

        // Listas para filtros
        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Estados = new[] { "Todos", SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
        ViewBag.EsAdministrador = SecurityHelper.IsAdministrator(User);

        return View(resultado);
    }

    [HttpGet("/equipos/ver/{id}")]
    public IActionResult Ver(int id)
    {
        var equipo = _equipoService.ObtenerPorId(id);
        if (equipo == null)
        {
            TempData["Error"] = "Equipo no encontrado";
            return RedirectToAction(nameof(Index));
        }

        return View(equipo);
    }

    [HttpGet("/equipos/crear")]
    public IActionResult Crear()
    {
        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Proveedores = _proveedorService.ObtenerActivos();
        ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
        ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };

        return View();
    }

    [HttpPost("/equipos/crear")]
    [ValidateAntiForgeryToken]
    public IActionResult Crear(Equipo equipo, int? ClienteId)
    {
        // Quitar validaciones automáticas innecesarias (codigo y propiedades de navegación)
        // para que no marquen como requeridos campos que vamos a manejar manualmente
        ModelState.Remove("Codigo");
        ModelState.Remove("CategoriaEquipo");
        ModelState.Remove("Ubicacion");

        // Validaciones manuales adicionales
        if (string.IsNullOrWhiteSpace(equipo.Nombre))
        {
            ModelState.AddModelError("Nombre", "El nombre del equipo es requerido");
        }

        if (equipo.CategoriaEquipoId <= 0)
        {
            ModelState.AddModelError("CategoriaEquipoId", "Debes seleccionar una categoría");
        }

        if (equipo.UbicacionId <= 0)
        {
            ModelState.AddModelError("UbicacionId", "Debes seleccionar una ubicación");
        }

        if (equipo.Stock < 0)
        {
            ModelState.AddModelError("Stock", "El stock no puede ser negativo");
        }

        if (string.IsNullOrWhiteSpace(equipo.Estado))
        {
            equipo.Estado = SD.EstadoEquipoDisponible; // Valor por defecto
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }

        var equipoCreado = _equipoService.Crear(equipo);
        
        string mensaje = $"✅ Equipo '{equipoCreado.Nombre}' (Código: {equipoCreado.Codigo}) creado exitosamente";

        // Log para depuración
        Console.WriteLine($"DEBUG - ClienteId recibido: {ClienteId?.ToString() ?? "NULL"}");

        // Si se seleccionó un cliente, crear la asignación automáticamente
        if (ClienteId.HasValue && ClienteId.Value > 0)
        {
            try
            {
                Console.WriteLine($"DEBUG - Entrando a crear asignación para ClienteId: {ClienteId.Value}");
                
                var cliente = _clienteService.ObtenerPorId(ClienteId.Value);
                Console.WriteLine($"DEBUG - Cliente encontrado: {cliente?.Nombre ?? "NULL"}");
                
                if (cliente != null)
                {
                    var asignacion = new AsignacionEquipo
                    {
                        EquipoId = equipoCreado.Id,
                        ClienteId = ClienteId.Value,
                        Cantidad = 1,
                        FechaAsignacion = DateTime.UtcNow,
                        Estado = "Activa",
                        Observaciones = $"Asignación automática al crear el equipo",
                        FechaCreacion = DateTime.UtcNow
                    };

                    Console.WriteLine($"DEBUG - Creando asignación...");
                    var asignacionCreada = _asignacionEquipoService.Crear(asignacion);
                    Console.WriteLine($"DEBUG - Asignación creada con ID: {asignacionCreada.Id}");
                    
                    // Actualizar el estado del equipo a "En uso" si está disponible
                    if (equipoCreado.Estado == SD.EstadoEquipoDisponible)
                    {
                        equipoCreado.Estado = SD.EstadoEquipoEnUso;
                        _equipoService.Actualizar(equipoCreado);
                        Console.WriteLine($"DEBUG - Estado del equipo actualizado a En uso");
                    }

                    mensaje += $" y asignado al cliente '{cliente.Codigo} - {cliente.Nombre}'";
                }
                else
                {
                    Console.WriteLine($"DEBUG - Cliente no encontrado con ID: {ClienteId.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG - ERROR al crear asignación: {ex.Message}");
                Console.WriteLine($"DEBUG - Stack trace: {ex.StackTrace}");
                mensaje += $" (⚠️ No se pudo crear la asignación: {ex.Message})";
            }
        }
        else
        {
            Console.WriteLine($"DEBUG - No se seleccionó cliente o ClienteId es 0/null");
        }

        TempData["Success"] = mensaje;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/equipos/editar/{id}")]
    public IActionResult Editar(int id)
    {
        var equipo = _equipoService.ObtenerPorId(id);
        if (equipo == null)
        {
            TempData["Error"] = "Equipo no encontrado";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
        ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
        ViewBag.Proveedores = _proveedorService.ObtenerActivos();
        ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
        ViewBag.AsignacionActiva = _asignacionEquipoService.ObtenerPorEquipo(id).FirstOrDefault(a => a.Estado == "Activa");
        ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };

        return View(equipo);
    }

    [HttpPost("/equipos/editar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(int id, Equipo equipo, int? ClienteId)
    {
        // Evitar errores de validación automática sobre propiedades de navegación
        // que no se envían en el formulario (solo se mandan los Ids).
        ModelState.Remove("CategoriaEquipo");
        ModelState.Remove("Ubicacion");
        ModelState.Remove("Proveedor");

        if (id != equipo.Id)
        {
            TempData["Error"] = "ID de equipo no coincide";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            ViewBag.AsignacionActiva = _asignacionEquipoService.ObtenerPorEquipo(id).FirstOrDefault(a => a.Estado == "Activa");
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }

        try
        {
            Console.WriteLine($"DEBUG EDITAR - ClienteId recibido: {ClienteId?.ToString() ?? "NULL"}");
            
            string mensaje = $"✅ Equipo '{equipo.Nombre}' actualizado exitosamente";

            // Manejar asignación de cliente ANTES de actualizar el equipo
            var asignacionActual = _asignacionEquipoService.ObtenerPorEquipo(id).FirstOrDefault(a => a.Estado == "Activa");
            
            Console.WriteLine($"DEBUG EDITAR - Asignación actual: {(asignacionActual != null ? $"Cliente {asignacionActual.ClienteId}" : "NULL")}");
            
            if (ClienteId.HasValue && ClienteId.Value > 0)
            {
                Console.WriteLine($"DEBUG EDITAR - Procesando asignación para ClienteId: {ClienteId.Value}");
                
                // Si hay un cliente seleccionado
                if (asignacionActual != null && asignacionActual.ClienteId != ClienteId.Value)
                {
                    // Cambiar asignación: devolver la actual y crear nueva
                    Console.WriteLine($"DEBUG EDITAR - Cambiando asignación");
                    _asignacionEquipoService.Devolver(asignacionActual.Id, DateTime.UtcNow);
                    
                    var cliente = _clienteService.ObtenerPorId(ClienteId.Value);
                    var nuevaAsignacion = new AsignacionEquipo
                    {
                        EquipoId = equipo.Id,
                        ClienteId = ClienteId.Value,
                        Cantidad = 1,
                        FechaAsignacion = DateTime.UtcNow,
                        Estado = "Activa",
                        Observaciones = $"Reasignado desde edición de equipo",
                        FechaCreacion = DateTime.UtcNow
                    };
                    _asignacionEquipoService.Crear(nuevaAsignacion);
                    mensaje += $" y reasignado a '{cliente?.Codigo} - {cliente?.Nombre}'";
                    Console.WriteLine($"DEBUG EDITAR - Asignación cambiada exitosamente");
                }
                else if (asignacionActual == null)
                {
                    // Crear nueva asignación
                    Console.WriteLine($"DEBUG EDITAR - Creando nueva asignación");
                    var cliente = _clienteService.ObtenerPorId(ClienteId.Value);
                    var nuevaAsignacion = new AsignacionEquipo
                    {
                        EquipoId = equipo.Id,
                        ClienteId = ClienteId.Value,
                        Cantidad = 1,
                        FechaAsignacion = DateTime.UtcNow,
                        Estado = "Activa",
                        Observaciones = $"Asignado desde edición de equipo",
                        FechaCreacion = DateTime.UtcNow
                    };
                    _asignacionEquipoService.Crear(nuevaAsignacion);
                    
                    // Cambiar estado a "En uso" si está disponible
                    if (equipo.Estado == SD.EstadoEquipoDisponible)
                    {
                        equipo.Estado = SD.EstadoEquipoEnUso;
                    }
                    
                    mensaje += $" y asignado a '{cliente?.Codigo} - {cliente?.Nombre}'";
                    Console.WriteLine($"DEBUG EDITAR - Nueva asignación creada exitosamente");
                }
                else
                {
                    Console.WriteLine($"DEBUG EDITAR - El equipo ya está asignado al mismo cliente, no hacer nada");
                }
            }
            else if (asignacionActual != null)
            {
                // Si no hay cliente seleccionado pero había una asignación, devolverla
                Console.WriteLine($"DEBUG EDITAR - Removiendo asignación");
                _asignacionEquipoService.Devolver(asignacionActual.Id, DateTime.UtcNow);
                mensaje += " y asignación removida";
            }

            // Actualizar el equipo UNA SOLA VEZ al final
            Console.WriteLine($"DEBUG EDITAR - Actualizando equipo con estado: {equipo.Estado}");
            _equipoService.Actualizar(equipo);
            Console.WriteLine($"DEBUG EDITAR - Equipo actualizado exitosamente");

            TempData["Success"] = mensaje;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            ViewBag.Categorias = _categoriaEquipoService.ObtenerActivas();
            ViewBag.Ubicaciones = _ubicacionService.ObtenerActivas();
            ViewBag.Proveedores = _proveedorService.ObtenerActivos();
            ViewBag.Estados = new[] { SD.EstadoEquipoDisponible, SD.EstadoEquipoEnUso, SD.EstadoEquipoDanado, SD.EstadoEquipoEnReparacion, SD.EstadoEquipoRetirado };
            return View(equipo);
        }
    }

    [HttpPost("/equipos/eliminar/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult Eliminar(int id)
    {
        try
        {
            var equipo = _equipoService.ObtenerPorId(id);
            if (equipo == null)
            {
                TempData["Error"] = "Equipo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            // Verificar si tiene asignaciones activas para el mensaje
            var asignacionActiva = equipo.AsignacionesEquipo?.FirstOrDefault(a => a.Estado == SD.EstadoAsignacionActiva);
            string mensajeAsignacion = "";
            
            if (asignacionActiva != null)
            {
                if (asignacionActiva.Cliente != null)
                {
                    mensajeAsignacion = $" (antes asignado al cliente {asignacionActiva.Cliente.Codigo} - {asignacionActiva.Cliente.Nombre})";
                }
                else if (!string.IsNullOrEmpty(asignacionActiva.EmpleadoNombre))
                {
                    mensajeAsignacion = $" (antes asignado al empleado {asignacionActiva.EmpleadoNombre})";
                }
            }

            if (_equipoService.Eliminar(id))
            {
                TempData["Success"] = $"✅ Equipo '{equipo.Nombre}' eliminado exitosamente{mensajeAsignacion}. Se eliminaron todos los registros relacionados (asignaciones, historial, movimientos y mantenimientos).";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar el equipo. Intenta nuevamente.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al eliminar: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/equipos/exportar-excel")]
    public IActionResult ExportarExcel()
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // Obtener todos los equipos con sus relaciones
                var equipos = _equipoService.ObtenerTodos()
                    .OrderBy(e => e.Nombre)
                    .ToList();

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Equipos");

                    // Encabezados
                    worksheet.Cells[1, 1].Value = "Código";
                    worksheet.Cells[1, 2].Value = "Nombre";
                    worksheet.Cells[1, 3].Value = "Categoría";
                    worksheet.Cells[1, 4].Value = "Ubicación";
                    worksheet.Cells[1, 5].Value = "Marca";
                    worksheet.Cells[1, 6].Value = "Modelo";
                    worksheet.Cells[1, 7].Value = "Número de Serie";
                    worksheet.Cells[1, 8].Value = "Proveedor";
                    worksheet.Cells[1, 9].Value = "Stock";
                    worksheet.Cells[1, 10].Value = "Stock Mínimo";
                    worksheet.Cells[1, 11].Value = "Precio Compra";
                    worksheet.Cells[1, 12].Value = "Estado";
                    worksheet.Cells[1, 13].Value = "Fecha Adquisición";
                    worksheet.Cells[1, 14].Value = "Fecha Creación";

                    // Formato encabezados (simple, sin depender de System.Drawing)
                    using (var range = worksheet.Cells[1, 1, 1, 14])
                    {
                        range.Style.Font.Bold = true;
                    }

                    // Datos
                    int row = 2;
                    foreach (var e in equipos)
                    {
                        worksheet.Cells[row, 1].Value = e.Codigo;
                        worksheet.Cells[row, 2].Value = e.Nombre;
                        worksheet.Cells[row, 3].Value = e.CategoriaEquipo?.Nombre ?? "";
                        worksheet.Cells[row, 4].Value = e.Ubicacion?.Nombre ?? "";
                        worksheet.Cells[row, 5].Value = e.Marca ?? "";
                        worksheet.Cells[row, 6].Value = e.Modelo ?? "";
                        worksheet.Cells[row, 7].Value = e.NumeroSerie ?? "";
                        worksheet.Cells[row, 8].Value = e.Proveedor?.Nombre ?? "";
                        worksheet.Cells[row, 9].Value = e.Stock;
                        worksheet.Cells[row, 10].Value = e.StockMinimo;
                        worksheet.Cells[row, 11].Value = e.PrecioCompra;
                        worksheet.Cells[row, 12].Value = e.Estado;
                        worksheet.Cells[row, 13].Value = e.FechaAdquisicion?.ToString("dd/MM/yyyy") ?? "";
                        worksheet.Cells[row, 14].Value = e.FechaCreacion.ToString("dd/MM/yyyy HH:mm");

                        row++;
                    }

                    // Ajustar ancho de columnas básico
                    for (int col = 1; col <= 14; col++)
                    {
                        worksheet.Column(col).AutoFit();
                    }

                    var nombreArchivo = $"Equipos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(package.GetAsByteArray(), contentType, nombreArchivo);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al exportar equipos a Excel: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
    }

    [HttpPost("/equipos/cambiar-estado")]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEstado(int equipoId, string nuevoEstado, string? motivo = null)
    {
        try
        {
            var usuarioId = SecurityHelper.GetUserId(User) ?? 0;
            if (_equipoService.CambiarEstado(equipoId, nuevoEstado, usuarioId, motivo))
            {
                TempData["Success"] = "Estado del equipo actualizado exitosamente";
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el estado del equipo";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Ver), new { id = equipoId });
    }

    // NUEVAS FUNCIONALIDADES - ACCIONES RÁPIDAS

    [HttpPost("/equipos/toggle-activo/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActivo(int id)
    {
        try
        {
            // Obtener el equipo directamente del contexto sin cargar relaciones
            var equipo = _context.Equipos.Find(id);
            if (equipo == null)
            {
                TempData["Error"] = "Equipo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            // Actualizar solo el campo Activo directamente
            equipo.Activo = !equipo.Activo;
            equipo.FechaActualizacion = DateTime.UtcNow;
            
            // Guardar cambios sin tocar las asignaciones
            _context.SaveChanges();

            var estado = equipo.Activo ? "habilitado" : "deshabilitado";
            TempData["Success"] = $"✅ Equipo '{equipo.Nombre}' {estado} exitosamente";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/equipos/devolver-asignacion/{id}")]
    [ValidateAntiForgeryToken]
    public IActionResult DevolverAsignacion(int id)
    {
        try
        {
            var equipo = _equipoService.ObtenerPorId(id);
            if (equipo == null)
            {
                TempData["Error"] = "Equipo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            var asignacionActiva = equipo.AsignacionesEquipo?.FirstOrDefault(a => a.Estado == SD.EstadoAsignacionActiva);
            if (asignacionActiva == null)
            {
                TempData["Error"] = "Este equipo no tiene asignaciones activas";
                return RedirectToAction(nameof(Index));
            }

            // Obtener información del cliente/empleado para el mensaje
            string asignadoA = "";
            if (asignacionActiva.Cliente != null)
            {
                asignadoA = $"cliente {asignacionActiva.Cliente.Codigo} - {asignacionActiva.Cliente.Nombre}";
            }
            else if (!string.IsNullOrEmpty(asignacionActiva.EmpleadoNombre))
            {
                asignadoA = $"empleado {asignacionActiva.EmpleadoNombre}";
            }

            // Devolver la asignación
            _asignacionEquipoService.Devolver(asignacionActiva.Id, DateTime.UtcNow);

            // Cambiar estado del equipo a Disponible
            equipo.Estado = SD.EstadoEquipoDisponible;
            equipo.FechaActualizacion = DateTime.UtcNow;
            _equipoService.Actualizar(equipo);

            TempData["Success"] = $"✅ Equipo '{equipo.Nombre}' devuelto exitosamente. Antes asignado a {asignadoA}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al devolver asignación: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/equipos/cambiar-estado-rapido")]
    [ValidateAntiForgeryToken]
    public IActionResult CambiarEstadoRapido(int equipoId, string nuevoEstado)
    {
        try
        {
            var equipo = _equipoService.ObtenerPorId(equipoId);
            if (equipo == null)
            {
                TempData["Error"] = "Equipo no encontrado";
                return RedirectToAction(nameof(Index));
            }

            var estadoAnterior = equipo.Estado;
            var usuarioId = SecurityHelper.GetUserId(User) ?? 0;
            
            if (_equipoService.CambiarEstado(equipoId, nuevoEstado, usuarioId, $"Cambio rápido desde listado"))
            {
                TempData["Success"] = $"✅ Estado cambiado de '{estadoAnterior}' a '{nuevoEstado}'";
            }
            else
            {
                TempData["Error"] = "No se pudo cambiar el estado";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/equipos/historial/{id}")]
    public IActionResult ObtenerHistorial(int id)
    {
        try
        {
            var equipo = _equipoService.ObtenerPorId(id);
            if (equipo == null)
            {
                return Json(new { success = false, message = "Equipo no encontrado" });
            }

            // Obtener historial de asignaciones
            var asignaciones = _context.AsignacionesEquipo
                .Where(a => a.EquipoId == id)
                .Include(a => a.Cliente)
                .OrderByDescending(a => a.FechaAsignacion)
                .Select(a => new
                {
                    tipo = "asignacion",
                    fecha = a.FechaAsignacion,
                    estado = a.Estado,
                    asignadoA = a.Cliente != null ? $"{a.Cliente.Codigo} - {a.Cliente.Nombre}" : a.EmpleadoNombre,
                    tipoAsignado = a.Cliente != null ? "Cliente" : "Empleado",
                    fechaDevolucion = a.FechaDevolucionReal,
                    observaciones = a.Observaciones
                })
                .ToList();

            // Obtener historial de estados
            var estadosHistorial = _context.HistorialEstadosEquipo
                .Where(h => h.EquipoId == id)
                .Include(h => h.Usuario)
                .OrderByDescending(h => h.FechaCambio)
                .Select(h => new
                {
                    tipo = "estado",
                    fecha = h.FechaCambio,
                    estadoAnterior = h.EstadoAnterior,
                    estadoNuevo = h.EstadoNuevo,
                    usuario = h.Usuario.NombreCompleto,
                    motivo = h.Motivo
                })
                .ToList();

            // Obtener mantenimientos/reparaciones
            var mantenimientos = _context.MantenimientosReparaciones
                .Where(m => m.EquipoId == id)
                .OrderByDescending(m => m.FechaCreacion)
                .Select(m => new
                {
                    tipo = "mantenimiento",
                    fecha = m.FechaCreacion,
                    tipoMantenimiento = m.Tipo,
                    descripcion = m.ProblemaReportado,
                    costo = m.Costo,
                    estado = m.Estado,
                    proveedor = m.ProveedorTecnico,
                    fechaResolucion = m.FechaFin
                })
                .ToList();

            return Json(new
            {
                success = true,
                equipo = new
                {
                    codigo = equipo.Codigo,
                    nombre = equipo.Nombre,
                    estado = equipo.Estado,
                    activo = equipo.Activo
                },
                asignaciones = asignaciones,
                estados = estadosHistorial,
                mantenimientos = mantenimientos
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


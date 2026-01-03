using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class MaterialInstalacionService : IMaterialInstalacionService
{
    private readonly ApplicationDbContext _context;
    private readonly IEquipoService _equipoService;
    private readonly IMovimientoInventarioService _movimientoInventarioService;

    public MaterialInstalacionService(ApplicationDbContext context, IEquipoService equipoService, IMovimientoInventarioService movimientoInventarioService)
    {
        _context = context;
        _equipoService = equipoService;
        _movimientoInventarioService = movimientoInventarioService;
    }

    public List<MaterialInstalacion> ObtenerPorCliente(int clienteId)
    {
        return _context.MaterialesInstalacion
            .Include(m => m.Equipo)
                .ThenInclude(e => e.CategoriaEquipo)
            .Where(m => m.ClienteId == clienteId)
            .OrderByDescending(m => m.FechaInstalacion)
            .ToList();
    }

    public MaterialInstalacion? ObtenerPorId(int id)
    {
        return _context.MaterialesInstalacion
            .Include(m => m.Equipo)
            .Include(m => m.Cliente)
            .FirstOrDefault(m => m.Id == id);
    }

    public MaterialInstalacion Crear(MaterialInstalacion material)
    {
        material.FechaCreacion = DateTime.Now;
        if (material.FechaInstalacion == default)
        {
            material.FechaInstalacion = DateTime.Now;
        }

        _context.MaterialesInstalacion.Add(material);
        _context.SaveChanges();
        return material;
    }

    public bool Eliminar(int id)
    {
        var material = ObtenerPorId(id);
        if (material == null)
            return false;

        _context.MaterialesInstalacion.Remove(material);
        _context.SaveChanges();
        return true;
    }

    /// <summary>
    /// Elimina un material de instalación y devuelve el stock al inventario
    /// </summary>
    /// <param name="id">ID del material de instalación a eliminar</param>
    /// <param name="usuarioId">ID del usuario que realiza la operación (opcional)</param>
    /// <returns>True si se eliminó correctamente, False si no se encontró</returns>
    public bool EliminarConDevolucionStock(int id, int? usuarioId = null)
    {
        var material = ObtenerPorId(id);
        if (material == null)
            return false;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var equipo = _equipoService.ObtenerPorId(material.EquipoId);
            if (equipo == null)
                throw new Exception($"Equipo con ID {material.EquipoId} no encontrado");

            var cliente = _context.Clientes.Find(material.ClienteId);
            if (cliente == null)
                throw new Exception($"Cliente con ID {material.ClienteId} no encontrado");

            // Obtener usuario por defecto si no se proporciona
            if (!usuarioId.HasValue)
            {
                var usuarioAdmin = _context.Usuarios.FirstOrDefault(u => u.Rol == SD.RolAdministrador);
                usuarioId = usuarioAdmin?.Id ?? 1;
            }

            // Devolver stock al inventario
            equipo.Stock += material.Cantidad;
            equipo.FechaActualizacion = DateTime.Now;

            // Crear movimiento de inventario (Entrada - Devolución)
            var movimiento = new MovimientoInventario
            {
                Tipo = SD.TipoMovimientoEntrada,
                Subtipo = SD.SubtipoMovimientoDevolucion,
                Fecha = DateTime.Now,
                UsuarioId = usuarioId.Value,
                Observaciones = $"Devolución de material de instalación: {equipo.Nombre} - Cliente: {cliente.Nombre} ({cliente.Codigo})",
                FechaCreacion = DateTime.Now
            };

            _context.MovimientosInventario.Add(movimiento);
            _context.SaveChanges(); // Guardar para obtener el ID del movimiento

            // Crear EquipoMovimiento para el movimiento de inventario
            var equipoMov = new EquipoMovimiento
            {
                MovimientoInventarioId = movimiento.Id,
                EquipoId = equipo.Id,
                Cantidad = (int)Math.Round(material.Cantidad), // Convertir decimal a int
                UbicacionOrigenId = equipo.UbicacionId
            };

            _context.EquipoMovimientos.Add(equipoMov);

            // Eliminar el material de instalación
            _context.MaterialesInstalacion.Remove(material);

            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void EliminarPorCliente(int clienteId)
    {
        var materiales = _context.MaterialesInstalacion
            .Where(m => m.ClienteId == clienteId)
            .ToList();

        _context.MaterialesInstalacion.RemoveRange(materiales);
        _context.SaveChanges();
    }

    /// <summary>
    /// Crea múltiples materiales de instalación y descuenta automáticamente del inventario
    /// </summary>
    /// <param name="clienteId">ID del cliente</param>
    /// <param name="materiales">Diccionario: EquipoId -> Cantidad</param>
    /// <param name="fechaInstalacion">Fecha de la instalación (por defecto: ahora)</param>
    /// <param name="usuarioId">ID del usuario que realiza la operación (opcional, para movimientos de inventario)</param>
    public void CrearMaterialesInstalacion(int clienteId, Dictionary<int, decimal> materiales, DateTime fechaInstalacion = default, int? usuarioId = null)
    {
        if (materiales == null || !materiales.Any())
            return;

        if (fechaInstalacion == default)
            fechaInstalacion = DateTime.Now;

        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var cliente = _context.Clientes.Find(clienteId);
            if (cliente == null)
                throw new Exception($"Cliente con ID {clienteId} no encontrado");

            // Obtener usuario por defecto si no se proporciona (usuario del sistema)
            if (!usuarioId.HasValue)
            {
                var usuarioAdmin = _context.Usuarios.FirstOrDefault(u => u.Rol == SD.RolAdministrador);
                usuarioId = usuarioAdmin?.Id ?? 1; // Fallback a ID 1 si no hay admin
            }

            var equipoMovimientos = new List<EquipoMovimiento>();

            foreach (var item in materiales)
            {
                var equipoId = item.Key;
                var cantidad = item.Value;

                if (cantidad <= 0)
                    continue;

                var equipo = _equipoService.ObtenerPorId(equipoId);
                if (equipo == null)
                    throw new Exception($"Equipo con ID {equipoId} no encontrado");

                // Validar stock disponible
                if (equipo.Stock < cantidad)
                    throw new Exception($"Stock insuficiente para '{equipo.Nombre}'. Stock disponible: {equipo.Stock} {equipo.UnidadMedida}, solicitado: {cantidad} {equipo.UnidadMedida}");

                // Crear registro de material instalado
                var material = new MaterialInstalacion
                {
                    ClienteId = clienteId,
                    EquipoId = equipoId,
                    Cantidad = cantidad,
                    FechaInstalacion = fechaInstalacion,
                    FechaCreacion = DateTime.Now
                };

                _context.MaterialesInstalacion.Add(material);

                // Preparar EquipoMovimiento para el movimiento de inventario
                // Nota: EquipoMovimiento.Cantidad es int, así que redondeamos si es necesario
                // Pero para materiales de instalación, debemos usar la cantidad exacta (puede ser decimal)
                // Como EquipoMovimiento.Cantidad es int, redondeamos para el movimiento
                var equipoMov = new EquipoMovimiento
                {
                    EquipoId = equipoId,
                    Cantidad = (int)Math.Round(cantidad), // Convertir decimal a int (redondeo)
                    UbicacionOrigenId = equipo.UbicacionId
                };
                equipoMovimientos.Add(equipoMov);

                // Descontar del inventario
                equipo.Stock -= cantidad;
                equipo.FechaActualizacion = DateTime.Now;

                if (equipo.Stock < 0)
                    equipo.Stock = 0; // Seguridad: no permitir stock negativo
            }

            // Guardar materiales primero
            _context.SaveChanges();

            // Crear movimiento de inventario manualmente (sin actualizar stock porque ya lo hicimos)
            if (equipoMovimientos.Any())
            {
                var movimiento = new MovimientoInventario
                {
                    Tipo = SD.TipoMovimientoSalida,
                    Subtipo = SD.SubtipoMovimientoAsignacion,
                    Fecha = fechaInstalacion,
                    UsuarioId = usuarioId.Value,
                    Observaciones = $"Materiales de instalación para cliente: {cliente.Nombre} ({cliente.Codigo})",
                    FechaCreacion = DateTime.Now
                };

                _context.MovimientosInventario.Add(movimiento);
                _context.SaveChanges(); // Guardar para obtener el ID del movimiento

                // Agregar EquipoMovimientos sin actualizar stock (ya está actualizado)
                foreach (var equipoMov in equipoMovimientos)
                {
                    equipoMov.MovimientoInventarioId = movimiento.Id;
                    _context.EquipoMovimientos.Add(equipoMov);
                }

                _context.SaveChanges();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}


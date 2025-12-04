using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IMovimientoInventarioService
{
    List<MovimientoInventario> ObtenerTodos();
    MovimientoInventario? ObtenerPorId(int id);
    List<MovimientoInventario> ObtenerPorEquipo(int equipoId);
    List<MovimientoInventario> ObtenerPorFecha(DateTime fechaInicio, DateTime fechaFin);
    MovimientoInventario Crear(MovimientoInventario movimiento, List<EquipoMovimiento> equipoMovimientos);
    bool Eliminar(int id);
}


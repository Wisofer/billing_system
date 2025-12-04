using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IMantenimientoReparacionService
{
    List<MantenimientoReparacion> ObtenerTodos();
    MantenimientoReparacion? ObtenerPorId(int id);
    List<MantenimientoReparacion> ObtenerPorEquipo(int equipoId);
    List<MantenimientoReparacion> ObtenerPorEstado(string estado);
    MantenimientoReparacion Crear(MantenimientoReparacion mantenimiento);
    MantenimientoReparacion Actualizar(MantenimientoReparacion mantenimiento);
    bool Eliminar(int id);
}


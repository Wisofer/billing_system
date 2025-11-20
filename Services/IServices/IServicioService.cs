using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IServicioService
{
    List<Servicio> ObtenerTodos();
    List<Servicio> ObtenerActivos();
    Servicio? ObtenerPorId(int id);
    Servicio Crear(Servicio servicio);
    Servicio Actualizar(Servicio servicio);
    bool Eliminar(int id);
}


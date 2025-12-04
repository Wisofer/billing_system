using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IUbicacionService
{
    List<Ubicacion> ObtenerTodas();
    List<Ubicacion> ObtenerActivas();
    Ubicacion? ObtenerPorId(int id);
    Ubicacion Crear(Ubicacion ubicacion);
    Ubicacion Actualizar(Ubicacion ubicacion);
    bool Eliminar(int id);
    bool ExisteNombre(string nombre, int? idExcluir = null);
}


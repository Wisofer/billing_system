using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IProveedorService
{
    List<Proveedor> ObtenerTodos();
    List<Proveedor> ObtenerActivos();
    Proveedor? ObtenerPorId(int id);
    Proveedor Crear(Proveedor proveedor);
    Proveedor Actualizar(Proveedor proveedor);
    bool Eliminar(int id);
    bool ExisteNombre(string nombre, int? idExcluir = null);
}


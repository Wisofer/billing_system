using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IMaterialInstalacionService
{
    List<MaterialInstalacion> ObtenerPorCliente(int clienteId);
    MaterialInstalacion? ObtenerPorId(int id);
    MaterialInstalacion Crear(MaterialInstalacion material);
    bool Eliminar(int id);
    bool EliminarConDevolucionStock(int id, int? usuarioId = null);
    void EliminarPorCliente(int clienteId);
    void CrearMaterialesInstalacion(int clienteId, Dictionary<int, decimal> materiales, DateTime fechaInstalacion = default, int? usuarioId = null);
}


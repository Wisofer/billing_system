using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IAsignacionEquipoService
{
    List<AsignacionEquipo> ObtenerTodas();
    List<AsignacionEquipo> ObtenerActivas();
    AsignacionEquipo? ObtenerPorId(int id);
    List<AsignacionEquipo> ObtenerPorEquipo(int equipoId);
    List<AsignacionEquipo> ObtenerPorCliente(int clienteId);
    AsignacionEquipo Crear(AsignacionEquipo asignacion);
    AsignacionEquipo Actualizar(AsignacionEquipo asignacion);
    bool Devolver(int asignacionId, DateTime fechaDevolucion);
    bool Eliminar(int id);
}


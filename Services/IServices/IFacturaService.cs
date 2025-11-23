using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IFacturaService
{
    List<Factura> ObtenerTodas();
    Factura? ObtenerPorId(int id);
    List<Factura> ObtenerPorCliente(int clienteId);
    List<Factura> ObtenerPorMes(DateTime mes);
    List<Factura> ObtenerPendientes();
    string GenerarNumeroFactura(Cliente cliente, DateTime mes);
    Factura Crear(Factura factura);
    Factura Actualizar(Factura factura);
    bool Eliminar(int id);
    (int eliminadas, int conPagos, int noEncontradas) EliminarMultiples(List<int> ids);
    void GenerarFacturasAutomaticas();
    decimal CalcularTotalPendiente();
    decimal CalcularTotalPagado();
    int ObtenerTotal();
    int ObtenerTotalPagadas();
    int ObtenerTotalPendientes();
    decimal ObtenerMontoTotal();
}


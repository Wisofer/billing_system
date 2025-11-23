using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IPagoService
{
    List<Pago> ObtenerTodos();
    Pago? ObtenerPorId(int id);
    List<Pago> ObtenerPorFactura(int facturaId);
    decimal CalcularVuelto(decimal montoRecibido, decimal costo);
    decimal ConvertirMoneda(decimal monto, string monedaOrigen, string monedaDestino);
    Pago Crear(Pago pago);
    Pago Actualizar(Pago pago);
    bool Eliminar(int id);
    (int eliminados, int noEncontrados) EliminarMultiples(List<int> ids);
    decimal CalcularTotalIngresos();
}


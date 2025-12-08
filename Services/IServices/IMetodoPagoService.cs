using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IMetodoPagoService
{
    /// <summary>
    /// Obtiene todos los métodos de pago
    /// </summary>
    List<MetodoPago> ObtenerTodos();
    
    /// <summary>
    /// Obtiene solo los métodos de pago activos
    /// </summary>
    List<MetodoPago> ObtenerActivos();
    
    /// <summary>
    /// Obtiene solo los métodos de pago activos ordenados por Orden (para landing page)
    /// </summary>
    List<MetodoPago> ObtenerActivosOrdenados();
    
    /// <summary>
    /// Obtiene un método de pago por ID
    /// </summary>
    MetodoPago? ObtenerPorId(int id);
    
    /// <summary>
    /// Crea un nuevo método de pago
    /// </summary>
    MetodoPago Crear(MetodoPago metodoPago);
    
    /// <summary>
    /// Actualiza un método de pago existente
    /// </summary>
    bool Actualizar(MetodoPago metodoPago);
    
    /// <summary>
    /// Elimina un método de pago
    /// </summary>
    bool Eliminar(int id);
    
    /// <summary>
    /// Actualiza el orden de múltiples métodos de pago
    /// </summary>
    bool ActualizarOrden(Dictionary<int, int> ordenPorId);
}


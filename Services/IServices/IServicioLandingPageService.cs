using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IServicioLandingPageService
{
    /// <summary>
    /// Obtiene todos los servicios de la landing page
    /// </summary>
    List<ServicioLandingPage> ObtenerTodos();
    
    /// <summary>
    /// Obtiene solo los servicios activos
    /// </summary>
    List<ServicioLandingPage> ObtenerActivos();
    
    /// <summary>
    /// Obtiene solo los servicios activos ordenados (para landing page)
    /// </summary>
    List<ServicioLandingPage> ObtenerActivosOrdenados();
    
    /// <summary>
    /// Obtiene un servicio por ID
    /// </summary>
    ServicioLandingPage? ObtenerPorId(int id);
    
    /// <summary>
    /// Crea un nuevo servicio
    /// </summary>
    ServicioLandingPage Crear(ServicioLandingPage servicio);
    
    /// <summary>
    /// Actualiza un servicio existente
    /// </summary>
    bool Actualizar(ServicioLandingPage servicio);
    
    /// <summary>
    /// Elimina un servicio
    /// </summary>
    bool Eliminar(int id);
    
    /// <summary>
    /// Actualiza el orden de múltiples servicios
    /// </summary>
    bool ActualizarOrden(Dictionary<int, int> ordenPorId);
}


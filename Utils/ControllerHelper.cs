using billing_system.Models.Entities;
using billing_system.Services.IServices;

namespace billing_system.Utils;

public static class ControllerHelper
{
    /// <summary>
    /// Configura ViewBag con clientes activos y servicios activos
    /// </summary>
    public static void SetClientesYServicios(dynamic viewBag, IClienteService clienteService, IServicioService servicioService)
    {
        viewBag.Clientes = clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        viewBag.Servicios = servicioService.ObtenerActivos();
    }
}


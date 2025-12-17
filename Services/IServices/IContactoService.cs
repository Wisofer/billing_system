using billing_system.Models.Entities;
using billing_system.Models.ViewModels;

namespace billing_system.Services.IServices
{
    public interface IContactoService
    {
        Task<Contacto> Crear(Contacto contacto);
        Task<Contacto?> ObtenerPorId(int id);
        Task<List<Contacto>> ObtenerTodos();
        Task<PagedResult<Contacto>> ObtenerPaginados(int pagina, int tamanoPagina, string? estado = null, string? busqueda = null);
        Task<Contacto?> MarcarComoLeido(int id);
        Task<Contacto?> MarcarComoRespondido(int id);
        Task<bool> Eliminar(int id);
        Task<int> ContarNuevos();
        Task<int> ContarPorEstado(string estado);
    }
}


using billing_system.Models.Entities;
using billing_system.Models.ViewModels;

namespace billing_system.Services.IServices;

public interface IClienteService
{
    List<Cliente> ObtenerTodos();
    PagedResult<Cliente> ObtenerPaginados(int pagina = 1, int tamanoPagina = 10, string? busqueda = null);
    Cliente? ObtenerPorId(int id);
    Cliente? ObtenerPorCodigo(string codigo);
    List<Cliente> Buscar(string termino);
    Cliente Crear(Cliente cliente);
    Cliente Actualizar(Cliente cliente);
    bool Eliminar(int id);
    bool ExisteCodigo(string codigo, int? idExcluir = null);
    int ObtenerTotal();
    int ObtenerTotalActivos();
    int ObtenerNuevosEsteMes();
}


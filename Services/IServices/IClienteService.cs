using billing_system.Models.Entities;

namespace billing_system.Services.IServices;

public interface IClienteService
{
    List<Cliente> ObtenerTodos();
    Cliente? ObtenerPorId(int id);
    Cliente? ObtenerPorCodigo(string codigo);
    List<Cliente> Buscar(string termino);
    Cliente Crear(Cliente cliente);
    Cliente Actualizar(Cliente cliente);
    bool Eliminar(int id);
    bool ExisteCodigo(string codigo, int? idExcluir = null);
}


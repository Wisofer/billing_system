using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;

namespace billing_system.Services;

public class ClienteService : IClienteService
{
    public List<Cliente> ObtenerTodos()
    {
        return InMemoryStorage.Clientes.ToList();
    }

    public Cliente? ObtenerPorId(int id)
    {
        return InMemoryStorage.Clientes.FirstOrDefault(c => c.Id == id);
    }

    public Cliente? ObtenerPorCodigo(string codigo)
    {
        return InMemoryStorage.Clientes.FirstOrDefault(c => c.Codigo == codigo);
    }

    public List<Cliente> Buscar(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return ObtenerTodos();

        termino = termino.ToLower();
        return InMemoryStorage.Clientes
            .Where(c => c.Nombre.ToLower().Contains(termino) ||
                       c.Codigo.ToLower().Contains(termino) ||
                       (c.Cedula != null && c.Cedula.ToLower().Contains(termino)) ||
                       (c.Telefono != null && c.Telefono.Contains(termino)))
            .ToList();
    }

    public Cliente Crear(Cliente cliente)
    {
        cliente.Id = InMemoryStorage.GetNextClienteId();
        cliente.FechaCreacion = DateTime.Now;
        cliente.Activo = true;
        
        // Generar código automáticamente si no viene
        if (string.IsNullOrWhiteSpace(cliente.Codigo))
        {
            var numeroCliente = InMemoryStorage.Clientes.Count + 1;
            cliente.Codigo = $"CLI-{numeroCliente:D3}";
            
            // Asegurar que el código sea único
            while (ExisteCodigo(cliente.Codigo))
            {
                numeroCliente++;
                cliente.Codigo = $"CLI-{numeroCliente:D3}";
            }
        }
        
        InMemoryStorage.Clientes.Add(cliente);
        return cliente;
    }

    public Cliente Actualizar(Cliente cliente)
    {
        var existente = ObtenerPorId(cliente.Id);
        if (existente == null)
            throw new Exception("Cliente no encontrado");

        existente.Codigo = cliente.Codigo;
        existente.Nombre = cliente.Nombre;
        existente.Telefono = cliente.Telefono;
        existente.Cedula = cliente.Cedula;
        existente.Email = cliente.Email;
        existente.Activo = cliente.Activo;

        return existente;
    }

    public bool Eliminar(int id)
    {
        var cliente = ObtenerPorId(id);
        if (cliente == null)
            return false;

        // Verificar si tiene facturas
        var tieneFacturas = InMemoryStorage.Facturas.Any(f => f.ClienteId == id);
        if (tieneFacturas)
            return false; // No se puede eliminar si tiene facturas

        InMemoryStorage.Clientes.Remove(cliente);
        return true;
    }

    public bool ExisteCodigo(string codigo, int? idExcluir = null)
    {
        return InMemoryStorage.Clientes
            .Any(c => c.Codigo == codigo && (idExcluir == null || c.Id != idExcluir));
    }
}


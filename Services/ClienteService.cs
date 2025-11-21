using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class ClienteService : IClienteService
{
    private readonly ApplicationDbContext _context;

    public ClienteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Cliente> ObtenerTodos()
    {
        return _context.Clientes
            .OrderByDescending(c => c.FechaCreacion)
            .ToList();
    }

    public PagedResult<Cliente> ObtenerPaginados(int pagina = 1, int tamanoPagina = 10, string? busqueda = null)
    {
        var query = _context.Clientes.AsQueryable();

        // Aplicar búsqueda si existe
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.ToLower();
            query = query.Where(c => 
                c.Nombre.ToLower().Contains(termino) ||
                c.Codigo.ToLower().Contains(termino) ||
                (c.Cedula != null && c.Cedula.ToLower().Contains(termino)) ||
                (c.Telefono != null && c.Telefono.Contains(termino)));
        }

        var totalItems = query.Count();

        var items = query
            .OrderByDescending(c => c.FechaCreacion)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToList();

        return new PagedResult<Cliente>
        {
            Items = items,
            CurrentPage = pagina,
            PageSize = tamanoPagina,
            TotalItems = totalItems
        };
    }

    public int ObtenerTotal()
    {
        return _context.Clientes.Count();
    }

    public int ObtenerTotalActivos()
    {
        return _context.Clientes.Count(c => c.Activo);
    }

    public int ObtenerNuevosEsteMes()
    {
        var ahora = DateTime.Now;
        return _context.Clientes
            .Count(c => c.FechaCreacion.Month == ahora.Month && c.FechaCreacion.Year == ahora.Year);
    }

    public Cliente? ObtenerPorId(int id)
    {
        return _context.Clientes.FirstOrDefault(c => c.Id == id);
    }

    public Cliente? ObtenerPorCodigo(string codigo)
    {
        return _context.Clientes.FirstOrDefault(c => c.Codigo == codigo);
    }

    public List<Cliente> Buscar(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
            return ObtenerTodos();

        termino = termino.ToLower();
        return _context.Clientes
            .Where(c => c.Nombre.ToLower().Contains(termino) ||
                       c.Codigo.ToLower().Contains(termino) ||
                       (c.Cedula != null && c.Cedula.ToLower().Contains(termino)) ||
                       (c.Telefono != null && c.Telefono.Contains(termino)))
            .OrderByDescending(c => c.FechaCreacion)
            .ToList();
    }

    public Cliente Crear(Cliente cliente)
    {
        cliente.FechaCreacion = DateTime.Now;
        cliente.Activo = true;
        
        // Generar código automáticamente si no viene
        if (string.IsNullOrWhiteSpace(cliente.Codigo))
        {
            var ultimoNumero = _context.Clientes
                .Where(c => c.Codigo.StartsWith("CLI-"))
                .Select(c => c.Codigo)
                .ToList()
                .Select(c => {
                    var partes = c.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[1], out var num))
                        return num;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();
            
            var numeroCliente = ultimoNumero + 1;
            cliente.Codigo = $"CLI-{numeroCliente:D3}";
            
            // Asegurar que el código sea único
            while (ExisteCodigo(cliente.Codigo))
            {
                numeroCliente++;
                cliente.Codigo = $"CLI-{numeroCliente:D3}";
            }
        }
        
        _context.Clientes.Add(cliente);
        _context.SaveChanges();
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

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var cliente = ObtenerPorId(id);
        if (cliente == null)
            return false;

        // Verificar si tiene facturas
        var tieneFacturas = _context.Facturas.Any(f => f.ClienteId == id);
        if (tieneFacturas)
            return false; // No se puede eliminar si tiene facturas

        _context.Clientes.Remove(cliente);
        _context.SaveChanges();
        return true;
    }

    public bool ExisteCodigo(string codigo, int? idExcluir = null)
    {
        return _context.Clientes
            .Any(c => c.Codigo == codigo && (idExcluir == null || c.Id != idExcluir));
    }
}


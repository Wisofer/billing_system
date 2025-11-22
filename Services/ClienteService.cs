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
        // Validar duplicados antes de crear
        if (ExisteNombreYCedula(cliente.Nombre, cliente.Cedula))
        {
            throw new Exception($"Ya existe un cliente con el nombre '{cliente.Nombre}'" + 
                              (string.IsNullOrWhiteSpace(cliente.Cedula) ? "" : $" y cédula '{cliente.Cedula}'"));
        }

        if (!string.IsNullOrWhiteSpace(cliente.Cedula) && ExisteCedula(cliente.Cedula))
        {
            throw new Exception($"Ya existe un cliente con la cédula '{cliente.Cedula}'");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Email) && ExisteEmail(cliente.Email))
        {
            throw new Exception($"Ya existe un cliente con el email '{cliente.Email}'");
        }

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

        // Validar duplicados antes de actualizar (excluyendo el cliente actual)
        if (ExisteNombreYCedula(cliente.Nombre, cliente.Cedula, cliente.Id))
        {
            throw new Exception($"Ya existe otro cliente con el nombre '{cliente.Nombre}'" + 
                              (string.IsNullOrWhiteSpace(cliente.Cedula) ? "" : $" y cédula '{cliente.Cedula}'"));
        }

        if (!string.IsNullOrWhiteSpace(cliente.Cedula) && ExisteCedula(cliente.Cedula, cliente.Id))
        {
            throw new Exception($"Ya existe otro cliente con la cédula '{cliente.Cedula}'");
        }

        if (!string.IsNullOrWhiteSpace(cliente.Email) && ExisteEmail(cliente.Email, cliente.Id))
        {
            throw new Exception($"Ya existe otro cliente con el email '{cliente.Email}'");
        }

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

    public bool ExisteCedula(string? cedula, int? idExcluir = null)
    {
        if (string.IsNullOrWhiteSpace(cedula))
            return false;
        
        return _context.Clientes
            .Any(c => c.Cedula != null && 
                     c.Cedula.Trim().ToUpper() == cedula.Trim().ToUpper() && 
                     (idExcluir == null || c.Id != idExcluir));
    }

    public bool ExisteEmail(string? email, int? idExcluir = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        return _context.Clientes
            .Any(c => c.Email != null && 
                     c.Email.Trim().ToLower() == email.Trim().ToLower() && 
                     (idExcluir == null || c.Id != idExcluir));
    }

    public bool ExisteNombreYCedula(string nombre, string? cedula, int? idExcluir = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return false;
        
        var nombreNormalizado = nombre.Trim().ToUpper();
        var query = _context.Clientes
            .Where(c => c.Nombre.Trim().ToUpper() == nombreNormalizado);
        
        if (!string.IsNullOrWhiteSpace(cedula))
        {
            var cedulaNormalizada = cedula.Trim().ToUpper();
            query = query.Where(c => c.Cedula != null && c.Cedula.Trim().ToUpper() == cedulaNormalizada);
        }
        
        if (idExcluir.HasValue)
        {
            query = query.Where(c => c.Id != idExcluir.Value);
        }
        
        return query.Any();
    }
}


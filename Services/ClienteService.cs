using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Utils;
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
            
            // Intentar parsear como número para buscar en TotalFacturas
            bool esNumero = int.TryParse(busqueda.Trim(), out int numeroFacturas);
            
            query = query.Where(c => 
                c.Nombre.ToLower().Contains(termino) ||
                c.Codigo.ToLower().Contains(termino) ||
                (c.Cedula != null && c.Cedula.ToLower().Contains(termino)) ||
                (c.Telefono != null && c.Telefono.Contains(termino)) ||
                (esNumero && c.TotalFacturas == numeroFacturas));
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
        return _context.Clientes
            .Include(c => c.Servicio)
            .FirstOrDefault(c => c.Id == id);
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
            cliente.Codigo = CodigoHelper.GenerarCodigoClienteUnico(codigo => ExisteCodigo(codigo));
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
        existente.FechaCreacion = cliente.FechaCreacion;
        existente.ServicioId = cliente.ServicioId;

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        try
        {
            var cliente = ObtenerPorId(id);
            if (cliente == null)
                return false;

            // Verificar si tiene facturas asociadas
            var tieneFacturas = _context.Facturas.Any(f => f.ClienteId == id);
            if (tieneFacturas)
                return false; // No se puede eliminar si tiene facturas

            _context.Clientes.Remove(cliente);
            _context.SaveChanges();
            return true;
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Error de restricción de clave foránea - el cliente tiene facturas o pagos asociados
            return false;
        }
        catch (Exception)
        {
            // Otro tipo de error
            return false;
        }
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

    public int ActualizarCodigosExistentes()
    {
        // Optimización: procesar en lotes para evitar cargar todos los clientes en memoria
        int actualizados = 0;
        const int batchSize = 100;
        int skip = 0;
        
        while (true)
        {
            var clientes = _context.Clientes
                .Where(c => !c.Codigo.StartsWith("EMS_"))
                .Skip(skip)
                .Take(batchSize)
                .ToList();

            if (!clientes.Any())
                break;

            foreach (var cliente in clientes)
            {
                var nuevoCodigo = CodigoHelper.GenerarCodigoClienteUnico(codigo => 
                    _context.Clientes.Any(c => c.Codigo == codigo && c.Id != cliente.Id));
                
                cliente.Codigo = nuevoCodigo;
                actualizados++;
            }

            _context.SaveChanges();
            skip += batchSize;
        }

        return actualizados;
    }
}


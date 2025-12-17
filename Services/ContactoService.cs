using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services
{
    public class ContactoService : IContactoService
    {
        private readonly ApplicationDbContext _context;

        public ContactoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Contacto> Crear(Contacto contacto)
        {
            contacto.FechaEnvio = DateTime.Now;
            contacto.Estado = "Nuevo";
            
            _context.Contactos.Add(contacto);
            await _context.SaveChangesAsync();
            
            return contacto;
        }

        public async Task<Contacto?> ObtenerPorId(int id)
        {
            return await _context.Contactos.FindAsync(id);
        }

        public async Task<List<Contacto>> ObtenerTodos()
        {
            return await _context.Contactos
                .OrderByDescending(c => c.FechaEnvio)
                .ToListAsync();
        }

        public async Task<PagedResult<Contacto>> ObtenerPaginados(int pagina, int tamanoPagina, string? estado = null, string? busqueda = null)
        {
            var query = _context.Contactos.AsQueryable();

            // Filtro por estado
            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                query = query.Where(c => c.Estado == estado);
            }

            // Filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                var terminoBusqueda = busqueda.ToLower();
                query = query.Where(c => 
                    c.Nombre.ToLower().Contains(terminoBusqueda) ||
                    c.Correo.ToLower().Contains(terminoBusqueda) ||
                    c.Telefono.Contains(busqueda) ||
                    c.Mensaje.ToLower().Contains(terminoBusqueda)
                );
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.FechaEnvio)
                .Skip((pagina - 1) * tamanoPagina)
                .Take(tamanoPagina)
                .ToListAsync();

            return new PagedResult<Contacto>
            {
                Items = items,
                CurrentPage = pagina,
                TotalItems = totalItems,
                PageSize = tamanoPagina
            };
        }

        public async Task<Contacto?> MarcarComoLeido(int id)
        {
            var contacto = await _context.Contactos.FindAsync(id);
            if (contacto == null) return null;

            if (contacto.Estado == "Nuevo")
            {
                contacto.Estado = "Leído";
                contacto.FechaLeido = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return contacto;
        }

        public async Task<Contacto?> MarcarComoRespondido(int id)
        {
            var contacto = await _context.Contactos.FindAsync(id);
            if (contacto == null) return null;

            contacto.Estado = "Respondido";
            contacto.FechaRespondido = DateTime.Now;
            if (contacto.FechaLeido == null)
            {
                contacto.FechaLeido = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            return contacto;
        }

        public async Task<bool> Eliminar(int id)
        {
            var contacto = await _context.Contactos.FindAsync(id);
            if (contacto == null) return false;

            _context.Contactos.Remove(contacto);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> ContarNuevos()
        {
            return await _context.Contactos.CountAsync(c => c.Estado == "Nuevo");
        }

        public async Task<int> ContarPorEstado(string estado)
        {
            return await _context.Contactos.CountAsync(c => c.Estado == estado);
        }
    }
}


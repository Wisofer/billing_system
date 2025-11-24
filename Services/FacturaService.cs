using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class FacturaService : IFacturaService
{
    private readonly ApplicationDbContext _context;
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;

    public FacturaService(ApplicationDbContext context, IClienteService clienteService, IServicioService servicioService)
    {
        _context = context;
        _clienteService = clienteService;
        _servicioService = servicioService;
    }

    public List<Factura> ObtenerTodas()
    {
        return _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Servicio)
            .OrderByDescending(f => f.FechaCreacion)
            .ToList();
    }

    public Factura? ObtenerPorId(int id)
    {
        return _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Servicio)
            .FirstOrDefault(f => f.Id == id);
    }

    public List<Factura> ObtenerPorCliente(int clienteId)
    {
        return _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Servicio)
            .Where(f => f.ClienteId == clienteId)
            .OrderByDescending(f => f.FechaCreacion)
            .ToList();
    }

    public List<Factura> ObtenerPorMes(DateTime mes)
    {
        return _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Servicio)
            .Where(f => f.MesFacturacion.Year == mes.Year && f.MesFacturacion.Month == mes.Month)
            .OrderByDescending(f => f.FechaCreacion)
            .ToList();
    }

    public List<Factura> ObtenerPendientes()
    {
        return _context.Facturas
            .Include(f => f.Cliente)
            .Include(f => f.Servicio)
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .OrderByDescending(f => f.FechaCreacion)
            .ToList();
    }

    public string GenerarNumeroFactura(Cliente cliente, DateTime mes)
    {
        var mesStr = mes.ToString("MM");
        var añoStr = mes.ToString("yyyy");
        
        // Obtener el último número de factura para este mes y año
        var ultimoNumero = _context.Facturas
            .Where(f => f.MesFacturacion.Year == mes.Year && f.MesFacturacion.Month == mes.Month)
            .OrderByDescending(f => f.Id)
            .Select(f => f.Numero)
            .FirstOrDefault();
        
        int numero = 1;
        if (!string.IsNullOrEmpty(ultimoNumero))
        {
            // Extraer el número del formato "XXXX-Nombre-MMYYYY"
            var partes = ultimoNumero.Split('-');
            if (partes.Length > 0 && int.TryParse(partes[0], out var num))
            {
                numero = num + 1;
            }
        }
        
        var nombreCliente = cliente.Nombre.Replace(" ", "").Substring(0, Math.Min(10, cliente.Nombre.Length));
        return $"{numero:D4}-{nombreCliente}-{mesStr}{añoStr}";
    }

    /// <summary>
    /// Calcula el monto de la factura aplicando lógica proporcional para clientes nuevos.
    /// El proporcional solo se aplica en la primera factura del cliente si inició después del día 5.
    /// Considera correctamente todos los meses (28, 29, 30, 31 días).
    /// </summary>
    private decimal CalcularMontoProporcional(Cliente cliente, Servicio servicio, DateTime mesFacturacion)
    {
        // Verificar si es la primera factura del cliente
        var esPrimeraFactura = !_context.Facturas.Any(f => f.ClienteId == cliente.Id);

        // Si no es la primera factura, siempre paga mes completo
        if (!esPrimeraFactura)
        {
            return servicio.Precio;
        }

        // Si es la primera factura, verificar si inició después del día 5
        var fechaInicioCliente = cliente.FechaCreacion.Date; // Normalizar a fecha sin hora
        
        // Si inició el día 5 o antes, paga mes completo
        if (fechaInicioCliente.Day <= 5)
        {
            return servicio.Precio;
        }

        // Si inició después del día 5, calcular proporcional
        // El ciclo de facturación es del día 5 de un mes al día 5 del siguiente mes
        // Calcular días desde la fecha de inicio hasta el día 5 del siguiente mes (incluyendo ambos días)
        
        // Obtener el día 5 del siguiente mes (mes de facturación + 1)
        var siguienteMes = mesFacturacion.AddMonths(1);
        var fechaVencimiento = new DateTime(siguienteMes.Year, siguienteMes.Month, 5);
        
        // Calcular días consumidos: desde fecha de inicio hasta día 5 del siguiente mes (incluyendo ambos días)
        // Ejemplo: del 23/nov al 5/dic = 13 días (23, 24, 25, 26, 27, 28, 29, 30 nov + 1, 2, 3, 4, 5 dic)
        var diasConsumidos = (fechaVencimiento.Date - fechaInicioCliente.Date).Days + 1;
        
        // Si los días son 0 o negativos, retornar precio completo (no debería pasar)
        if (diasConsumidos <= 0)
        {
            return servicio.Precio;
        }

        // Obtener días del mes de facturación (considera correctamente 28, 29, 30, 31 días)
        // DateTime.DaysInMonth maneja automáticamente años bisiestos (febrero con 29 días)
        var diasDelMes = DateTime.DaysInMonth(mesFacturacion.Year, mesFacturacion.Month);
        
        // Calcular costo por día (precio del servicio dividido entre los días del mes de facturación)
        var costoPorDia = servicio.Precio / diasDelMes;
        
        // Calcular monto proporcional
        var montoProporcional = diasConsumidos * costoPorDia;
        
        // Asegurar que el monto proporcional no exceda el precio completo (por seguridad)
        if (montoProporcional > servicio.Precio)
        {
            montoProporcional = servicio.Precio;
        }
        
        // Redondear a 2 decimales
        return Math.Round(montoProporcional, 2);
    }

    public Factura Crear(Factura factura)
    {
        var cliente = _clienteService.ObtenerPorId(factura.ClienteId);
        if (cliente == null)
            throw new Exception("Cliente no encontrado");

        var servicio = _servicioService.ObtenerPorId(factura.ServicioId);
        if (servicio == null)
            throw new Exception("Servicio no encontrado");

        factura.Numero = GenerarNumeroFactura(cliente, factura.MesFacturacion);
        // Calcular monto aplicando lógica proporcional si es necesario
        factura.Monto = CalcularMontoProporcional(cliente, servicio, factura.MesFacturacion);
        factura.Estado = SD.EstadoFacturaPendiente;
        factura.FechaCreacion = DateTime.Now;

        _context.Facturas.Add(factura);
        
        // Actualizar el último servicio usado del cliente
        cliente.ServicioId = factura.ServicioId;
        
        // NO incrementar TotalFacturas aquí - solo se incrementa cuando la factura se marca como Pagada
        
        _context.SaveChanges();
        return factura;
    }

    public Factura Actualizar(Factura factura)
    {
        var existente = ObtenerPorId(factura.Id);
        if (existente == null)
            throw new Exception("Factura no encontrada");

        var estadoAnterior = existente.Estado;
        var estadoNuevo = factura.Estado;

        // Solo actualizar campos permitidos
        existente.Estado = factura.Estado;
        if (!string.IsNullOrWhiteSpace(factura.ArchivoPDF))
        {
            existente.ArchivoPDF = factura.ArchivoPDF;
        }

        // Incrementar TotalFacturas solo cuando la factura cambia a estado "Pagada"
        if (estadoAnterior != SD.EstadoFacturaPagada && estadoNuevo == SD.EstadoFacturaPagada)
        {
            var cliente = _clienteService.ObtenerPorId(existente.ClienteId);
            if (cliente != null)
            {
                cliente.TotalFacturas++;
            }
        }
        // Decrementar si cambia de Pagada a otro estado (aunque esto no debería pasar normalmente)
        else if (estadoAnterior == SD.EstadoFacturaPagada && estadoNuevo != SD.EstadoFacturaPagada)
        {
            var cliente = _clienteService.ObtenerPorId(existente.ClienteId);
            if (cliente != null && cliente.TotalFacturas > 0)
            {
                cliente.TotalFacturas--;
            }
        }

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var factura = ObtenerPorId(id);
        if (factura == null)
            return false;

        // Verificar si tiene pagos
        var tienePagos = _context.Pagos.Any(p => p.FacturaId == id);
        if (tienePagos)
            return false; // No se puede eliminar si tiene pagos

        // Decrementar TotalFacturas solo si la factura estaba pagada
        if (factura.Estado == SD.EstadoFacturaPagada)
        {
            var cliente = _clienteService.ObtenerPorId(factura.ClienteId);
            if (cliente != null && cliente.TotalFacturas > 0)
            {
                cliente.TotalFacturas--;
            }
        }

        _context.Facturas.Remove(factura);
        _context.SaveChanges();
        return true;
    }

    public (int eliminadas, int conPagos, int noEncontradas) EliminarMultiples(List<int> ids)
    {
        if (ids == null || !ids.Any())
            return (0, 0, 0);

        int eliminadas = 0;
        int conPagos = 0;
        int noEncontradas = 0;

        foreach (var id in ids)
        {
            var factura = ObtenerPorId(id);
            if (factura == null)
            {
                noEncontradas++;
                continue;
            }

            // Verificar si tiene pagos
            var tienePagos = _context.Pagos.Any(p => p.FacturaId == id);
            if (tienePagos)
            {
                conPagos++;
                continue;
            }

            // Decrementar TotalFacturas solo si la factura estaba pagada
            if (factura.Estado == SD.EstadoFacturaPagada)
            {
                var cliente = _clienteService.ObtenerPorId(factura.ClienteId);
                if (cliente != null && cliente.TotalFacturas > 0)
                {
                    cliente.TotalFacturas--;
                }
            }

            _context.Facturas.Remove(factura);
            eliminadas++;
        }

        if (eliminadas > 0)
        {
            _context.SaveChanges();
        }

        return (eliminadas, conPagos, noEncontradas);
    }

    public void GenerarFacturasAutomaticas()
    {
        var clientes = _clienteService.ObtenerTodos().Where(c => c.Activo && c.ServicioId.HasValue).ToList();
        var mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        if (!clientes.Any())
        {
            return; // No hay clientes activos con servicio asignado
        }

        var facturasACrear = new List<Factura>();
        int contadorFacturas = 0;

        foreach (var cliente in clientes)
        {
            // Solo generar factura para el servicio asignado al cliente (ServicioId)
            if (!cliente.ServicioId.HasValue)
            {
                continue; // Saltar clientes sin servicio asignado
            }

            var servicioId = cliente.ServicioId.Value;
            var servicio = _servicioService.ObtenerPorId(servicioId);
            
            // Verificar que el servicio existe y está activo
            if (servicio == null || !servicio.Activo)
            {
                continue; // Saltar si el servicio no existe o no está activo
            }

            // Verificar si ya existe factura para este cliente y servicio en este mes
            var existe = _context.Facturas.Any(f =>
                f.ClienteId == cliente.Id &&
                f.ServicioId == servicioId &&
                f.MesFacturacion.Year == mesActual.Year &&
                f.MesFacturacion.Month == mesActual.Month);

            if (!existe)
            {
                contadorFacturas++;
                var mesStr = mesActual.ToString("MM");
                var añoStr = mesActual.ToString("yyyy");
                var nombreCliente = cliente.Nombre.Replace(" ", "").Substring(0, Math.Min(10, cliente.Nombre.Length));
                
                // Calcular monto aplicando lógica proporcional si es necesario
                var monto = CalcularMontoProporcional(cliente, servicio, mesActual);
                
                var factura = new Factura
                {
                    ClienteId = cliente.Id,
                    ServicioId = servicioId,
                    MesFacturacion = mesActual,
                    Numero = $"{contadorFacturas:D4}-{nombreCliente}-{mesStr}{añoStr}",
                    Monto = monto,
                    Estado = SD.EstadoFacturaPendiente,
                    FechaCreacion = DateTime.Now
                };
                facturasACrear.Add(factura);
            }
        }

        // Guardar todas las facturas en una sola transacción
        if (facturasACrear.Any())
        {
            // NO incrementar TotalFacturas aquí - solo se incrementa cuando la factura se marca como Pagada
            _context.Facturas.AddRange(facturasACrear);
            _context.SaveChanges();
        }
    }

    public decimal CalcularTotalPendiente()
    {
        return _context.Facturas
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);
    }

    public decimal CalcularTotalPagado()
    {
        return _context.Pagos.Sum(p => p.Monto);
    }

    public int ObtenerTotal()
    {
        return _context.Facturas.Count();
    }

    public int ObtenerTotalPagadas()
    {
        return _context.Facturas.Count(f => f.Estado == SD.EstadoFacturaPagada);
    }

    public int ObtenerTotalPendientes()
    {
        return _context.Facturas.Count(f => f.Estado == SD.EstadoFacturaPendiente);
    }

    public decimal ObtenerMontoTotal()
    {
        return _context.Facturas.Sum(f => f.Monto);
    }
}


using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;

namespace billing_system.Services;

public class FacturaService : IFacturaService
{
    private readonly IClienteService _clienteService;
    private readonly IServicioService _servicioService;

    public FacturaService(IClienteService clienteService, IServicioService servicioService)
    {
        _clienteService = clienteService;
        _servicioService = servicioService;
    }

    public List<Factura> ObtenerTodas()
    {
        return InMemoryStorage.Facturas.ToList();
    }

    public Factura? ObtenerPorId(int id)
    {
        return InMemoryStorage.Facturas.FirstOrDefault(f => f.Id == id);
    }

    public List<Factura> ObtenerPorCliente(int clienteId)
    {
        return InMemoryStorage.Facturas.Where(f => f.ClienteId == clienteId).ToList();
    }

    public List<Factura> ObtenerPorMes(DateTime mes)
    {
        return InMemoryStorage.Facturas
            .Where(f => f.MesFacturacion.Year == mes.Year && f.MesFacturacion.Month == mes.Month)
            .ToList();
    }

    public List<Factura> ObtenerPendientes()
    {
        return InMemoryStorage.Facturas
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .ToList();
    }

    public string GenerarNumeroFactura(Cliente cliente, DateTime mes)
    {
        var mesStr = mes.ToString("MM");
        var añoStr = mes.ToString("yyyy");
        var numero = InMemoryStorage.Facturas.Count + 1;
        return $"{numero:D4}-{cliente.Nombre.Replace(" ", "")}-{mesStr}{añoStr}";
    }

    public Factura Crear(Factura factura)
    {
        var cliente = _clienteService.ObtenerPorId(factura.ClienteId);
        if (cliente == null)
            throw new Exception("Cliente no encontrado");

        var servicio = _servicioService.ObtenerPorId(factura.ServicioId);
        if (servicio == null)
            throw new Exception("Servicio no encontrado");

        factura.Id = InMemoryStorage.GetNextFacturaId();
        factura.Numero = GenerarNumeroFactura(cliente, factura.MesFacturacion);
        factura.Monto = servicio.Precio;
        factura.Estado = SD.EstadoFacturaPendiente;
        factura.FechaCreacion = DateTime.Now;

        InMemoryStorage.Facturas.Add(factura);
        return factura;
    }

    public Factura Actualizar(Factura factura)
    {
        var existente = ObtenerPorId(factura.Id);
        if (existente == null)
            throw new Exception("Factura no encontrada");

        existente.Estado = factura.Estado;
        existente.ArchivoPDF = factura.ArchivoPDF;

        return existente;
    }

    public bool Eliminar(int id)
    {
        var factura = ObtenerPorId(id);
        if (factura == null)
            return false;

        // Verificar si tiene pagos
        var tienePagos = InMemoryStorage.Pagos.Any(p => p.FacturaId == id);
        if (tienePagos)
            return false; // No se puede eliminar si tiene pagos

        InMemoryStorage.Facturas.Remove(factura);
        return true;
    }

    public void GenerarFacturasAutomaticas()
    {
        var clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        var servicios = _servicioService.ObtenerActivos();
        var mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        foreach (var cliente in clientes)
        {
            foreach (var servicio in servicios)
            {
                // Verificar si ya existe factura para este cliente y servicio en este mes
                var existe = InMemoryStorage.Facturas.Any(f =>
                    f.ClienteId == cliente.Id &&
                    f.ServicioId == servicio.Id &&
                    f.MesFacturacion.Year == mesActual.Year &&
                    f.MesFacturacion.Month == mesActual.Month);

                if (!existe)
                {
                    var factura = new Factura
                    {
                        ClienteId = cliente.Id,
                        ServicioId = servicio.Id,
                        MesFacturacion = mesActual
                    };
                    Crear(factura);
                }
            }
        }
    }

    public decimal CalcularTotalPendiente()
    {
        return InMemoryStorage.Facturas
            .Where(f => f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);
    }

    public decimal CalcularTotalPagado()
    {
        return InMemoryStorage.Pagos.Sum(p => p.Monto);
    }
}


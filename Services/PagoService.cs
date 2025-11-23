using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

namespace billing_system.Services;

public class PagoService : IPagoService
{
    private readonly ApplicationDbContext _context;
    private readonly IFacturaService _facturaService;

    public PagoService(ApplicationDbContext context, IFacturaService facturaService)
    {
        _context = context;
        _facturaService = facturaService;
    }

    public List<Pago> ObtenerTodos()
    {
        return _context.Pagos
            .Include(p => p.Factura)
            .ThenInclude(f => f!.Cliente)
            .OrderByDescending(p => p.FechaPago)
            .ToList();
    }

    public Pago? ObtenerPorId(int id)
    {
        return _context.Pagos
            .Include(p => p.Factura)
            .ThenInclude(f => f!.Cliente)
            .FirstOrDefault(p => p.Id == id);
    }

    public List<Pago> ObtenerPorFactura(int facturaId)
    {
        return _context.Pagos
            .Where(p => p.FacturaId == facturaId)
            .OrderByDescending(p => p.FechaPago)
            .ToList();
    }

    public decimal CalcularVuelto(decimal montoRecibido, decimal costo)
    {
        return montoRecibido > costo ? montoRecibido - costo : 0;
    }

    public decimal ConvertirMoneda(decimal monto, string monedaOrigen, string monedaDestino)
    {
        if (monedaOrigen == monedaDestino)
            return monto;

        if (monedaOrigen == SD.MonedaCordoba && monedaDestino == SD.MonedaDolar)
            return monto / SD.TipoCambioDolar;

        if (monedaOrigen == SD.MonedaDolar && monedaDestino == SD.MonedaCordoba)
            return monto * SD.TipoCambioDolar;

        return monto;
    }

    public Pago Crear(Pago pago)
    {
        var factura = _facturaService.ObtenerPorId(pago.FacturaId);
        if (factura == null)
            throw new Exception("Factura no encontrada");

        pago.FechaPago = DateTime.Now;
        pago.TipoCambio = SD.TipoCambioDolar;

        // Calcular vuelto si es pago físico
        if (pago.TipoPago == SD.TipoPagoFisico && pago.MontoRecibido.HasValue)
        {
            pago.Vuelto = CalcularVuelto(pago.MontoRecibido.Value, pago.Monto);
        }

        _context.Pagos.Add(pago);
        _context.SaveChanges();

        // Actualizar estado de factura si está completamente pagada
        var totalPagado = ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
        if (totalPagado >= factura.Monto)
        {
            factura.Estado = SD.EstadoFacturaPagada;
            _context.SaveChanges();
        }

        return pago;
    }

    public Pago Actualizar(Pago pago)
    {
        var existente = ObtenerPorId(pago.Id);
        if (existente == null)
            throw new Exception("Pago no encontrado");

        existente.Monto = pago.Monto;
        existente.Moneda = pago.Moneda;
        existente.TipoPago = pago.TipoPago;
        existente.Banco = pago.Banco;
        existente.TipoCuenta = pago.TipoCuenta;
        existente.MontoRecibido = pago.MontoRecibido;
        existente.Vuelto = pago.Vuelto;
        existente.Observaciones = pago.Observaciones;

        if (pago.TipoPago == SD.TipoPagoFisico && pago.MontoRecibido.HasValue)
        {
            existente.Vuelto = CalcularVuelto(pago.MontoRecibido.Value, pago.Monto);
        }

        _context.SaveChanges();
        return existente;
    }

    public bool Eliminar(int id)
    {
        var pago = ObtenerPorId(id);
        if (pago == null)
            return false;

        var facturaId = pago.FacturaId;
        _context.Pagos.Remove(pago);
        _context.SaveChanges();

        // Actualizar estado de factura
        var factura = _facturaService.ObtenerPorId(facturaId);
        if (factura != null)
        {
            var totalPagado = ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
            if (totalPagado < factura.Monto)
            {
                factura.Estado = SD.EstadoFacturaPendiente;
                _context.SaveChanges();
            }
        }

        return true;
    }

    public (int eliminados, int noEncontrados) EliminarMultiples(List<int> ids)
    {
        if (ids == null || !ids.Any())
            return (0, 0);

        int eliminados = 0;
        int noEncontrados = 0;
        var facturasARevisar = new HashSet<int>();

        foreach (var id in ids)
        {
            var pago = ObtenerPorId(id);
            if (pago == null)
            {
                noEncontrados++;
                continue;
            }

            facturasARevisar.Add(pago.FacturaId);
            _context.Pagos.Remove(pago);
            eliminados++;
        }

        if (eliminados > 0)
        {
            _context.SaveChanges();

            // Actualizar estado de facturas afectadas
            foreach (var facturaId in facturasARevisar)
            {
                var factura = _facturaService.ObtenerPorId(facturaId);
                if (factura != null)
                {
                    var totalPagado = ObtenerPorFactura(factura.Id).Sum(p => p.Monto);
                    if (totalPagado < factura.Monto)
                    {
                        factura.Estado = SD.EstadoFacturaPendiente;
                    }
                }
            }
            _context.SaveChanges();
        }

        return (eliminados, noEncontrados);
    }

    public decimal CalcularTotalIngresos()
    {
        return _context.Pagos.Sum(p => p.Monto);
    }
}


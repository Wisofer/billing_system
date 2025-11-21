namespace billing_system.Services;

public interface IPdfService
{
    byte[] GenerarPdfFactura(Models.Entities.Factura factura);
}


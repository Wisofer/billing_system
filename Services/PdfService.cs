using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using billing_system.Models.Entities;

namespace billing_system.Services;

public class PdfService : IPdfService
{
    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarPdfFactura(Factura factura)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("FACTURA").FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Text($"Número: {factura.Numero}").FontSize(12);
                            column.Item().Text($"Fecha: {factura.FechaCreacion:dd/MM/yyyy}").FontSize(10);
                        });

                        row.ConstantItem(100).AlignRight().Column(column =>
                        {
                            column.Item().Text("Estado:").FontSize(10);
                            column.Item().PaddingTop(2).Text(factura.Estado).FontSize(12).Bold()
                                .FontColor(factura.Estado == "Pagada" ? Colors.Green.Darken2 : 
                                          factura.Estado == "Cancelada" ? Colors.Red.Darken2 : 
                                          Colors.Orange.Darken2);
                        });
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        // Información del Cliente
                        column.Item().BorderBottom(1).PaddingBottom(10).Column(clienteColumn =>
                        {
                            clienteColumn.Item().Text("DATOS DEL CLIENTE").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                            clienteColumn.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Código: {factura.Cliente.Codigo}").FontSize(10);
                                row.RelativeItem().Text($"Nombre: {factura.Cliente.Nombre}").FontSize(10);
                            });
                            if (!string.IsNullOrEmpty(factura.Cliente.Telefono))
                            {
                                clienteColumn.Item().Text($"Teléfono: {factura.Cliente.Telefono}").FontSize(10);
                            }
                            if (!string.IsNullOrEmpty(factura.Cliente.Email))
                            {
                                clienteColumn.Item().Text($"Email: {factura.Cliente.Email}").FontSize(10);
                            }
                        });

                        // Detalles de la Factura
                        column.Item().BorderBottom(1).PaddingBottom(10).Column(detallesColumn =>
                        {
                            detallesColumn.Item().Text("DETALLES DE LA FACTURA").FontSize(14).Bold().FontColor(Colors.Grey.Darken3);
                            detallesColumn.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Servicio: {factura.Servicio.Nombre}").FontSize(10);
                                row.RelativeItem().Text($"Mes: {factura.MesFacturacion:MMMM yyyy}").FontSize(10);
                            });
                        });

                        // Tabla de Montos
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Concepto").FontSize(11).Bold();
                                header.Cell().Element(CellStyle).AlignRight().Text("Monto").FontSize(11).Bold();
                            });

                            table.Cell().Element(CellStyle).Text(factura.Servicio.Nombre).FontSize(10);
                            table.Cell().Element(CellStyle).AlignRight().Text($"C$ {factura.Monto:N2}").FontSize(10);

                            table.Cell().Element(CellStyle).Text("TOTAL").FontSize(12).Bold();
                            table.Cell().Element(CellStyle).AlignRight().Text($"C$ {factura.Monto:N2}").FontSize(12).Bold();
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Sistema de Facturación - ").FontSize(8).FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        });

        return documento.GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(5);
    }
}


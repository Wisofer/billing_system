using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using billing_system.Models.Entities;
using billing_system.Utils;
using billing_system.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace billing_system.Services;

public class PdfService : IPdfService
{
    private readonly IWebHostEnvironment? _environment;
    private readonly ApplicationDbContext? _context;

    public PdfService(IWebHostEnvironment? environment = null, ApplicationDbContext? context = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        _environment = environment;
        _context = context;
    }

    /// <summary>
    /// Genera un PDF simplificado para facturas de Streaming (sin proporcional, sin multa)
    /// </summary>
    public byte[] GenerarPdfFacturaStreaming(Factura factura)
    {
        var fechaEmision = DateTime.Now;
        var mesFacturado = factura.MesFacturacion.ToString("MMM/yyyy").ToUpper();
        
        // Cargar logo si existe
        byte[]? logoBytes = null;
        if (_environment != null)
        {
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo.png");
            if (File.Exists(logoPath))
            {
                logoBytes = File.ReadAllBytes(logoPath);
            }
        }
        
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header simplificado
                page.Header()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            // Logo (si existe)
                            if (logoBytes != null)
                            {
                                row.ConstantItem(80).Height(60).AlignLeft().AlignMiddle()
                                    .Image(logoBytes).FitArea();
                            }
                            else
                            {
                                row.ConstantItem(80);
                            }
                            
                            // Información de la Empresa
                            row.RelativeItem().PaddingLeft(10).Column(empresaColumn =>
                            {
                                empresaColumn.Item().Text("Servicios de Streaming").FontSize(16).Bold().FontColor(Colors.Purple.Darken3);
                                empresaColumn.Item().Text("Sistema de Facturación").FontSize(9).FontColor(Colors.Grey.Darken2);
                                empresaColumn.Item().Text("Nicaragua").FontSize(8).FontColor(Colors.Grey.Medium);
                                empresaColumn.Item().Text("Correo: atencion.al.cliente@emsinetsolut.com").FontSize(8).FontColor(Colors.Grey.Medium);
                                empresaColumn.Item().Text("Teléfonos: 89308058 / 82771485").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            // Número de Factura destacado - Formato F-0001-STR
                            row.RelativeItem().Column(facturaColumn =>
                            {
                                // Extraer el número del formato "0001-Nombre-MMYYYY-STR" y mostrar como "F-0001-STR"
                                var numeroFactura = "F-0001-STR";
                                var partes = factura.Numero.Split('-');
                                if (partes.Length > 0 && int.TryParse(partes[0], out var num))
                                {
                                    numeroFactura = $"F-{num:D4}-STR";
                                }
                                facturaColumn.Item().Text(numeroFactura).FontSize(14).Bold().FontColor(Colors.Purple.Darken3);
                            });

                            // Estado
                            row.ConstantItem(100).AlignRight().Column(estadoColumn =>
                            {
                                estadoColumn.Item().Text("Estado:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                estadoColumn.Item().PaddingTop(2).Text(factura.Estado).FontSize(11).Bold()
                                    .FontColor(factura.Estado == "Pagada" ? Colors.Green.Darken2 : 
                                              factura.Estado == "Cancelada" ? Colors.Red.Darken2 : 
                                              Colors.Orange.Darken2);
                            });
                        });
                    });

                page.Content()
                    .PaddingVertical(0.8f, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Información del Cliente (simplificada)
                        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Column(clienteColumn =>
                            {
                                clienteColumn.Item().Text("DATOS DEL CLIENTE").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                                clienteColumn.Item().PaddingTop(5).Row(clienteRow =>
                                {
                                    clienteRow.RelativeItem().Column(datosColumn =>
                                    {
                                        datosColumn.Item().Text($"Cliente: {factura.Cliente.Nombre}").FontSize(9);
                                        if (!string.IsNullOrEmpty(factura.Cliente.Cedula))
                                        {
                                            datosColumn.Item().Text($"Cédula: {factura.Cliente.Cedula}").FontSize(9);
                                        }
                                        datosColumn.Item().Text($"Código: {factura.Cliente.Codigo}").FontSize(9);
                                        datosColumn.Item().Text($"Fecha: {fechaEmision:dd/MM/yyyy}").FontSize(9);
                                    });
                                    clienteRow.RelativeItem().Column(contactoColumn =>
                                    {
                                        if (!string.IsNullOrEmpty(factura.Cliente.Telefono))
                                        {
                                            contactoColumn.Item().Text($"Teléfono: {factura.Cliente.Telefono}").FontSize(9);
                                        }
                                        if (!string.IsNullOrEmpty(factura.Cliente.Email))
                                        {
                                            contactoColumn.Item().Text($"E-mail: {factura.Cliente.Email}").FontSize(9);
                                        }
                                        contactoColumn.Item().Text($"Mes facturado: {mesFacturado}").FontSize(9);
                                    });
                                });
                            });

                        // Descripción del Servicio Consumido (simplificada)
                        column.Item().Column(servicioColumn =>
                        {
                            servicioColumn.Item().Text("Descripción del servicio consumido").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                            servicioColumn.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(120);
                                });

                                // Header de la tabla
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Descripción del servicio consumido").FontSize(10).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Monto C$").FontSize(10).Bold();
                                });

                                // Mostrar servicios
                                if (factura.FacturaServicios != null && factura.FacturaServicios.Any())
                                {
                                    foreach (var facturaServicio in factura.FacturaServicios)
                                    {
                                        var servicio = facturaServicio.Servicio;
                                        
                                        // Si tiene múltiples suscripciones, mostrar cada una
                                        if (facturaServicio.Cantidad > 1)
                                        {
                                            var precioUnitario = servicio.Precio;
                                            for (int i = 1; i <= facturaServicio.Cantidad; i++)
                                            {
                                                table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                                {
                                                    servicioInfoColumn.Item().Text($"{servicio.Nombre} - Suscripción {i}").FontSize(9);
                                                    if (!string.IsNullOrWhiteSpace(servicio.Descripcion))
                                                    {
                                                        servicioInfoColumn.Item().PaddingTop(2).Text(servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                                    }
                                                });
                                                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{precioUnitario:N2}").FontSize(9);
                                            }
                                        }
                                        else
                                        {
                                            table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                            {
                                                servicioInfoColumn.Item().Text(servicio.Nombre).FontSize(9);
                                                if (!string.IsNullOrWhiteSpace(servicio.Descripcion))
                                                {
                                                    servicioInfoColumn.Item().PaddingTop(2).Text(servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                            table.Cell().Element(BodyCellStyle).AlignRight().Text($"{servicio.Precio:N2}").FontSize(9);
                                        }
                                    }
                                }
                                else
                                {
                                    // Fallback: mostrar servicio principal
                                    table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                    {
                                        servicioInfoColumn.Item().Text(factura.Servicio.Nombre).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(factura.Servicio.Descripcion))
                                        {
                                            servicioInfoColumn.Item().PaddingTop(2).Text(factura.Servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                        }
                                    });
                                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Servicio.Precio:N2}").FontSize(9);
                                }

                                // Sub-total (sin proporcional para Streaming)
                                table.Cell().Element(BodyCellStyle).Text("Sub-total C$").FontSize(9);
                                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(9);

                                // IVA
                                table.Cell().Element(BodyCellStyle).Text("I.V.A. C$").FontSize(9);
                                table.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                // Método de pago (si hay pagos) - después de IVA
                                if (factura.Pagos != null && factura.Pagos.Any())
                                {
                                    var primerPago = factura.Pagos.OrderBy(p => p.FechaPago).First();
                                    string metodoPagoTexto = "";
                                    if (primerPago.TipoPago == SD.TipoPagoFisico)
                                        metodoPagoTexto = "Físico";
                                    else if (primerPago.TipoPago == SD.TipoPagoElectronico)
                                        metodoPagoTexto = "Electrónico";
                                    else if (primerPago.TipoPago == SD.TipoPagoMixto)
                                        metodoPagoTexto = "Mixto";
                                    
                                    if (!string.IsNullOrEmpty(metodoPagoTexto))
                                    {
                                        table.Cell().Element(BodyCellStyle).Text("Método de pago").FontSize(9);
                                        table.Cell().Element(BodyCellStyle).AlignRight().Text(metodoPagoTexto).FontSize(9).Bold();
                                    }
                                }

                                // Total
                                table.Cell().Element(TotalCellStyle).Text("Total C$").FontSize(11).Bold();
                                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(11).Bold();
                            });
                        });

                        // Sección de Métodos de Pago - Compacta y visual
                        column.Item().PaddingTop(10).PaddingBottom(5).Column(pagoColumn =>
                        {
                            pagoColumn.Item()
                                .Background(Colors.Purple.Lighten5)
                                .Border(2)
                                .BorderColor(Colors.Purple.Darken2)
                                .Padding(10)
                                .Column(metodoPagoColumn =>
                                {
                                    metodoPagoColumn.Item()
                                        .AlignCenter()
                                        .Text("💳 MÉTODOS DE PAGO")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Purple.Darken3);
                                    
                                    metodoPagoColumn.Item().PaddingTop(5)
                                        .AlignCenter()
                                        .Text("Consulta los métodos de pago disponibles")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken2);
                                    
                                    metodoPagoColumn.Item().PaddingTop(5)
                                        .AlignCenter()
                                        .Hyperlink("https://www.emsinetsolut.com/#pagos")
                                        .Text("https://www.emsinetsolut.com/#pagos")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.Purple.Darken2)
                                        .Underline();
                                });
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .PaddingTop(10)
                    .Text(x =>
                    {
                        x.Span("Sistema de Facturación Servicios de Streaming - ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
            });
        });

        return documento.GeneratePdf();
    }

    public byte[] GenerarPdfFactura(Factura factura)
    {
        // Si es Streaming, usar el método específico
        if (factura.Categoria == SD.CategoriaStreaming)
        {
            return GenerarPdfFacturaStreaming(factura);
        }
        
        // Fecha de emisión: usar la fecha de creación de la factura
        var fechaEmision = factura.FechaCreacion;
        // Fecha de vencimiento: siempre día 06 del mes de facturación
        var fechaVencimiento = new DateTime(factura.MesFacturacion.Year, factura.MesFacturacion.Month, 6);
        var mesFacturado = factura.MesFacturacion.ToString("MMM/yyyy").ToUpper();
        
        // Calcular multa: 200.00 solo si la factura está pendiente y la fecha actual es después del día 6
        var fechaActual = DateTime.Now;
        var multaPorPagoTardio = 0.00m;
        if (factura.Estado == SD.EstadoFacturaPendiente && fechaActual > fechaVencimiento)
        {
            multaPorPagoTardio = 200.00m;
        }
        
        // Cargar logo si existe
        byte[]? logoBytes = null;
        if (_environment != null)
        {
            var logoPath = Path.Combine(_environment.WebRootPath, "images", "logo.png");
            if (File.Exists(logoPath))
            {
                logoBytes = File.ReadAllBytes(logoPath);
            }
        }
        
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header con Logo e Información de la Empresa
                page.Header()
                    .Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            // Logo (si existe)
                            if (logoBytes != null)
                            {
                                row.ConstantItem(80).Height(60).AlignLeft().AlignMiddle()
                                    .Image(logoBytes).FitArea();
                            }
                            else
                            {
                                row.ConstantItem(80);
                            }
                            
                            // Información de la Empresa
                            row.RelativeItem().PaddingLeft(10).Column(empresaColumn =>
                            {
                                empresaColumn.Item().Text("Servicio De Internet").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                                empresaColumn.Item().Text("Sistema de Facturación").FontSize(9).FontColor(Colors.Grey.Darken2);
                                empresaColumn.Item().Text("Nicaragua").FontSize(8).FontColor(Colors.Grey.Medium);
                                empresaColumn.Item().Text("Correo: atencion.al.cliente@emsinetsolut.com").FontSize(8).FontColor(Colors.Grey.Medium);
                                empresaColumn.Item().Text("Teléfonos: 89308058 / 82771485").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });

                        column.Item().PaddingTop(10).Row(row =>
                        {
                            // Número de Factura destacado - Formato F-0001
                            row.RelativeItem().Column(facturaColumn =>
                            {
                                // Extraer el número del formato "0001-Nombre-MMYYYY" y mostrar como "F-0001"
                                var numeroFactura = "F-0001";
                                var partes = factura.Numero.Split('-');
                                if (partes.Length > 0 && int.TryParse(partes[0], out var num))
                                {
                                    numeroFactura = $"F-{num:D4}";
                                }
                                facturaColumn.Item().Text(numeroFactura).FontSize(14).Bold().FontColor(Colors.Blue.Darken3);
                            });

                            // Estado
                            row.ConstantItem(100).AlignRight().Column(estadoColumn =>
                            {
                                estadoColumn.Item().Text("Estado:").FontSize(9).FontColor(Colors.Grey.Darken2);
                                estadoColumn.Item().PaddingTop(2).Text(factura.Estado).FontSize(11).Bold()
                                    .FontColor(factura.Estado == "Pagada" ? Colors.Green.Darken2 : 
                                              factura.Estado == "Cancelada" ? Colors.Red.Darken2 : 
                                              Colors.Orange.Darken2);
                            });
                        });
                    });

                page.Content()
                    .PaddingVertical(0.8f, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Información del Cliente (formato similar a la imagen)
                        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Column(clienteColumn =>
                            {
                                clienteColumn.Item().Text("DATOS DEL CLIENTE").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                                clienteColumn.Item().PaddingTop(5).Row(clienteRow =>
                                {
                                    clienteRow.RelativeItem().Column(datosColumn =>
                                    {
                                        datosColumn.Item().Text($"Cliente: {factura.Cliente.Nombre}").FontSize(9);
                                        if (!string.IsNullOrEmpty(factura.Cliente.Cedula))
                                        {
                                            datosColumn.Item().Text($"Cédula: {factura.Cliente.Cedula}").FontSize(9);
                                        }
                                        datosColumn.Item().Text($"Código: {factura.Cliente.Codigo}").FontSize(9);
                                        datosColumn.Item().Text($"Fecha: {fechaEmision:dd/MM/yyyy}").FontSize(9);
                                    });
                                    clienteRow.RelativeItem().Column(contactoColumn =>
                                    {
                                        if (!string.IsNullOrEmpty(factura.Cliente.Telefono))
                                        {
                                            contactoColumn.Item().Text($"Teléfono: {factura.Cliente.Telefono}").FontSize(9);
                                        }
                                        if (!string.IsNullOrEmpty(factura.Cliente.Email))
                                        {
                                            contactoColumn.Item().Text($"E-mail: {factura.Cliente.Email}").FontSize(9);
                                        }
                                        contactoColumn.Item().Text($"Mes facturado: {mesFacturado}").FontSize(9);
                                        contactoColumn.Item().Text($"Vencimiento: {fechaVencimiento:dd/MM/yyyy}").FontSize(10).Bold().FontColor(Colors.Red.Darken2);
                                    });
                                });
                            });

                        // Descripción del Servicio Consumido
                        column.Item().Column(servicioColumn =>
                        {
                            servicioColumn.Item().Text("Descripción del servicio consumido").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                            servicioColumn.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(120);
                                });

                                // Header de la tabla
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Descripción del servicio consumido").FontSize(10).Bold();
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Monto C$").FontSize(10).Bold();
                                });

                                // Calcular subtotal sin proporcional para mostrar descuento si aplica
                                decimal subtotalSinProporcional = 0;
                                
                                // Si la factura tiene FacturaServicios (múltiples servicios), mostrar todos
                                if (factura.FacturaServicios != null && factura.FacturaServicios.Any())
                                {
                                    foreach (var facturaServicio in factura.FacturaServicios)
                                    {
                                        var servicio = facturaServicio.Servicio;
                                        
                                        // Si es Streaming y tiene múltiples suscripciones, mostrar cada una
                                        if (servicio.Categoria == SD.CategoriaStreaming && facturaServicio.Cantidad > 1)
                                        {
                                            // Mostrar el precio completo del servicio por cada suscripción
                                            var precioUnitario = servicio.Precio;
                                            subtotalSinProporcional += precioUnitario * facturaServicio.Cantidad;
                                            
                                            for (int i = 1; i <= facturaServicio.Cantidad; i++)
                                            {
                                                table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                                {
                                                    servicioInfoColumn.Item().Text($"{servicio.Nombre} - Suscripción {i}").FontSize(9);
                                                    if (!string.IsNullOrWhiteSpace(servicio.Descripcion))
                                                    {
                                                        servicioInfoColumn.Item().PaddingTop(2).Text(servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                                    }
                                                });
                                                table.Cell().Element(BodyCellStyle).AlignRight().Text($"{precioUnitario:N2}").FontSize(9);
                                            }
                                        }
                                        else
                                        {
                                            // Servicio único o Internet - mostrar precio completo
                                            var precioCompleto = servicio.Precio;
                                            subtotalSinProporcional += precioCompleto;
                                            
                                            table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                            {
                                                servicioInfoColumn.Item().Text(servicio.Nombre).FontSize(9);
                                                if (!string.IsNullOrWhiteSpace(servicio.Descripcion))
                                                {
                                                    servicioInfoColumn.Item().PaddingTop(2).Text(servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                                }
                                            });
                                            table.Cell().Element(BodyCellStyle).AlignRight().Text($"{precioCompleto:N2}").FontSize(9);
                                        }
                                    }
                                }
                                else
                                {
                                    // Fallback: mostrar servicio principal (compatibilidad con facturas antiguas)
                                    subtotalSinProporcional = factura.Servicio.Precio;
                                    table.Cell().Element(BodyCellStyle).Column(servicioInfoColumn =>
                                    {
                                        servicioInfoColumn.Item().Text(factura.Servicio.Nombre).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(factura.Servicio.Descripcion))
                                        {
                                            servicioInfoColumn.Item().PaddingTop(2).Text(factura.Servicio.Descripcion).FontSize(8).FontColor(Colors.Grey.Darken1);
                                        }
                                    });
                                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Servicio.Precio:N2}").FontSize(9);
                                }

                                // Sub-total (sin proporcional si aplica)
                                if (subtotalSinProporcional > 0 && Math.Abs(subtotalSinProporcional - factura.Monto) > 0.01m)
                                {
                                    table.Cell().Element(BodyCellStyle).Text("Sub-total C$").FontSize(9);
                                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{subtotalSinProporcional:N2}").FontSize(9);
                                    
                                    // Descuento proporcional
                                    var descuentoProporcional = subtotalSinProporcional - factura.Monto;
                                    if (descuentoProporcional > 0)
                                    {
                                        table.Cell().Element(BodyCellStyle).Text("Descuento proporcional C$").FontSize(9).FontColor(Colors.Red.Darken1);
                                        table.Cell().Element(BodyCellStyle).AlignRight().Text($"-{descuentoProporcional:N2}").FontSize(9).FontColor(Colors.Red.Darken1);
                                    }
                                }
                                else
                                {
                                    table.Cell().Element(BodyCellStyle).Text("Sub-total C$").FontSize(9);
                                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(9);
                                }

                                // IVA
                                table.Cell().Element(BodyCellStyle).Text("I.V.A. C$").FontSize(9);
                                table.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                // Método de pago (si hay pagos) - después de IVA
                                if (factura.Pagos != null && factura.Pagos.Any())
                                {
                                    var primerPago = factura.Pagos.OrderBy(p => p.FechaPago).First();
                                    string metodoPagoTexto = "";
                                    if (primerPago.TipoPago == SD.TipoPagoFisico)
                                        metodoPagoTexto = "Físico";
                                    else if (primerPago.TipoPago == SD.TipoPagoElectronico)
                                        metodoPagoTexto = "Electrónico";
                                    else if (primerPago.TipoPago == SD.TipoPagoMixto)
                                        metodoPagoTexto = "Mixto";
                                    
                                    if (!string.IsNullOrEmpty(metodoPagoTexto))
                                    {
                                        table.Cell().Element(BodyCellStyle).Text("Método de pago").FontSize(9);
                                        table.Cell().Element(BodyCellStyle).AlignRight().Text(metodoPagoTexto).FontSize(9).Bold();
                                    }
                                }

                                // Total
                                table.Cell().Element(TotalCellStyle).Text("Total C$").FontSize(11).Bold();
                                table.Cell().Element(TotalCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(11).Bold();
                            });
                        });

                        // Estado de la Cuenta cuando hay pagos
                        if (factura.Pagos != null && factura.Pagos.Any())
                        {
                            // Calcular el monto total pagado sumando los montos aplicados de cada pago
                            var montoPagado = 0m;
                            if (_context != null)
                            {
                                foreach (var pago in factura.Pagos)
                                {
                                    if (pago.FacturaId == factura.Id)
                                    {
                                        montoPagado += pago.Monto;
                                    }
                                    else
                                    {
                                        var pagoFactura = _context.PagoFacturas
                                            .FirstOrDefault(pf => pf.PagoId == pago.Id && pf.FacturaId == factura.Id);
                                        if (pagoFactura != null)
                                        {
                                            montoPagado += pagoFactura.MontoAplicado;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                montoPagado = factura.Pagos.Sum(p => p.Monto);
                            }
                            column.Item().Column(estadoColumn =>
                            {
                                estadoColumn.Item().Text("Estado de la cuenta").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                                estadoColumn.Item().PaddingTop(5).Table(estadoTable =>
                                {
                                    estadoTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.ConstantColumn(120);
                                    });

                                    estadoTable.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Estado de la cuenta").FontSize(10).Bold();
                                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("C$").FontSize(10).Bold();
                                    });

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Saldo inicial del periodo").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Monto consumido").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Multa por pago tardío").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{multaPorPagoTardio:N2}").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Monto pagado").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{montoPagado:N2}").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Saldo final del periodo").FontSize(9);
                                    var saldoFinal = factura.Monto - montoPagado + multaPorPagoTardio;
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{saldoFinal:N2}").FontSize(9);

                                    // Monto total a pagar: siempre mostrar el monto de la factura (monto consumido)
                                    var montoTotalPagar = factura.Monto;
                                    estadoTable.Cell().Element(TotalCellStyle).Text("Monto total a pagar").FontSize(11).Bold();
                                    estadoTable.Cell().Element(TotalCellStyle).AlignRight().Text($"{montoTotalPagar:N2}").FontSize(11).Bold();
                                });
                            });
                        }
                        else
                        {
                            // Estado de la cuenta sin pagos
                            column.Item().Column(estadoColumn =>
                            {
                                estadoColumn.Item().Text("Estado de la cuenta").FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                                estadoColumn.Item().PaddingTop(5).Table(estadoTable =>
                                {
                                    estadoTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.ConstantColumn(120);
                                    });

                                    estadoTable.Header(header =>
                                    {
                                        header.Cell().Element(HeaderCellStyle).Text("Estado de la cuenta").FontSize(10).Bold();
                                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("C$").FontSize(10).Bold();
                                    });

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Saldo inicial del periodo").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Monto consumido").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{factura.Monto:N2}").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Multa por pago tardío").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text($"{multaPorPagoTardio:N2}").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Monto pagado").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                    estadoTable.Cell().Element(BodyCellStyle).Text("Saldo final del periodo").FontSize(9);
                                    estadoTable.Cell().Element(BodyCellStyle).AlignRight().Text("0.00").FontSize(9);

                                    var montoTotalPagar = factura.Monto + multaPorPagoTardio;
                                    estadoTable.Cell().Element(TotalCellStyle).Text("Monto total a pagar").FontSize(11).Bold();
                                    estadoTable.Cell().Element(TotalCellStyle).AlignRight().Text($"{montoTotalPagar:N2}").FontSize(11).Bold();
                                });
                            });
                        }

                        // Sección de Métodos de Pago - Compacta y visual
                        column.Item().PaddingTop(10).PaddingBottom(5).Column(pagoColumn =>
                        {
                            pagoColumn.Item()
                                .Background(Colors.Blue.Lighten5)
                                .Border(2)
                                .BorderColor(Colors.Blue.Darken2)
                                .Padding(10)
                                .Column(metodoPagoColumn =>
                                {
                                    metodoPagoColumn.Item()
                                        .AlignCenter()
                                        .Text("💳 MÉTODOS DE PAGO")
                                        .FontSize(12)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken3);
                                    
                                    metodoPagoColumn.Item().PaddingTop(5)
                                        .AlignCenter()
                                        .Text("Consulta los métodos de pago disponibles")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken2);
                                    
                                    metodoPagoColumn.Item().PaddingTop(5)
                                        .AlignCenter()
                                        .Hyperlink("https://www.emsinetsolut.com/#pagos")
                                        .Text("https://www.emsinetsolut.com/#pagos")
                                        .FontSize(10)
                                        .Bold()
                                        .FontColor(Colors.Blue.Darken2)
                                        .Underline();
                                });
                        });
                    });

                page.Footer()
                    .AlignCenter()
                    .PaddingTop(10)
                    .Text(x =>
                    {
                        x.Span("Sistema de Facturación Servicio De Internet - ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
            });
        });

        return documento.GeneratePdf();
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(6)
            .PaddingHorizontal(10);
    }

    private static IContainer TotalCellStyle(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten4)
            .BorderTop(2)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Medium)
            .PaddingVertical(8)
            .PaddingHorizontal(10);
    }
}


using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Models.ViewModels;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace billing_system.Services;

public class ReporteService : IReporteService
{
    private readonly ApplicationDbContext _context;
    private readonly CultureInfo _cultura = new CultureInfo("es-NI");

    public ReporteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public ReporteMensualViewModel GenerarReportePorMes(int año, int mes)
    {
        var fechaInicio = new DateTime(año, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
        
        return GenerarReporteMensual(fechaInicio, fechaFin);
    }

    public ReporteMensualViewModel GenerarReporteMensual(DateTime fechaInicio, DateTime fechaFin)
    {
        // Ajustar fechas para incluir todo el rango
        fechaInicio = fechaInicio.Date;
        fechaFin = fechaFin.Date.AddDays(1).AddSeconds(-1);

        var reporte = new ReporteMensualViewModel
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            PeriodoTexto = ObtenerTextoPeriodo(fechaInicio, fechaFin)
        };

        // Obtener datos del periodo
        ObtenerDatosFacturas(reporte, fechaInicio, fechaFin);
        ObtenerDatosPagos(reporte, fechaInicio, fechaFin);
        ObtenerDatosEgresos(reporte, fechaInicio, fechaFin);
        ObtenerDatosClientes(reporte, fechaInicio, fechaFin);
        CalcularComparacionMesAnterior(reporte);

        // Calcular balance
        reporte.Balance = reporte.TotalIngresos - reporte.TotalEgresos;

        return reporte;
    }

    private void ObtenerDatosFacturas(ReporteMensualViewModel reporte, DateTime fechaInicio, DateTime fechaFin)
    {
        // Obtener facturas del periodo
        var facturas = _context.Facturas
            .Include(f => f.Cliente)
            .Where(f => f.FechaCreacion >= fechaInicio && f.FechaCreacion <= fechaFin)
            .OrderByDescending(f => f.FechaCreacion)
            .ToList();

        reporte.FacturasGeneradas = facturas.Count();
        reporte.FacturasPagadas = facturas.Count(f => f.Estado == SD.EstadoFacturaPagada);
        reporte.FacturasPendientes = facturas.Count(f => f.Estado == SD.EstadoFacturaPendiente);
        reporte.FacturasInternet = facturas.Count(f => f.Categoria == SD.CategoriaInternet);
        reporte.FacturasStreaming = facturas.Count(f => f.Categoria == SD.CategoriaStreaming);

        // Calcular pendientes por categoría
        reporte.PendientesInternet = facturas
            .Where(f => f.Categoria == SD.CategoriaInternet && f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);
        
        reporte.PendientesStreaming = facturas
            .Where(f => f.Categoria == SD.CategoriaStreaming && f.Estado == SD.EstadoFacturaPendiente)
            .Sum(f => f.Monto);

        // Lista de facturas para el reporte
        reporte.ListaFacturas = facturas.Select(f => new FacturaReporteDto
        {
            Id = f.Id,
            Numero = f.Numero,
            ClienteCodigo = f.Cliente?.Codigo ?? "",
            ClienteNombre = f.Cliente?.Nombre ?? "",
            Fecha = f.FechaCreacion,
            Monto = f.Monto,
            Estado = f.Estado,
            Categoria = f.Categoria
        }).ToList();
    }

    private void ObtenerDatosPagos(ReporteMensualViewModel reporte, DateTime fechaInicio, DateTime fechaFin)
    {
        // Obtener pagos del periodo
        var pagos = _context.Pagos
            .Include(p => p.Factura)
                .ThenInclude(f => f.Cliente)
            .Include(p => p.PagoFacturas)
                .ThenInclude(pf => pf.Factura)
                    .ThenInclude(f => f.Cliente)
            .Where(p => p.FechaPago >= fechaInicio && p.FechaPago <= fechaFin)
            .OrderByDescending(p => p.FechaPago)
            .ToList();

        reporte.PagosRecibidos = pagos.Count();
        reporte.MontoPagos = pagos.Sum(p => p.Monto);
        reporte.TotalIngresos = reporte.MontoPagos;

        // Calcular ingresos por categoría
        foreach (var pago in pagos)
        {
            // Si tiene Factura directa de Internet
            if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaInternet)
            {
                reporte.IngresosInternet += pago.Monto;
            }
            else if (pago.Factura != null && pago.Factura.Categoria == SD.CategoriaStreaming)
            {
                reporte.IngresosStreaming += pago.Monto;
            }
            
            // Si tiene PagoFacturas, sumar el MontoAplicado de cada una
            foreach (var pagoFactura in pago.PagoFacturas)
            {
                if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaInternet)
                {
                    reporte.IngresosInternet += pagoFactura.MontoAplicado;
                }
                else if (pagoFactura.Factura != null && pagoFactura.Factura.Categoria == SD.CategoriaStreaming)
                {
                    reporte.IngresosStreaming += pagoFactura.MontoAplicado;
                }
            }
        }

        // Lista de pagos para el reporte
        reporte.ListaPagos = pagos.Select(p => new PagoReporteDto
        {
            Id = p.Id,
            ClienteCodigo = p.Factura?.Cliente?.Codigo ?? "",
            ClienteNombre = p.Factura?.Cliente?.Nombre ?? "",
            Fecha = p.FechaPago,
            Monto = p.Monto,
            MetodoPago = p.TipoPago,
            NumeroReferencia = p.Observaciones ?? ""
        }).ToList();
    }

    private void ObtenerDatosEgresos(ReporteMensualViewModel reporte, DateTime fechaInicio, DateTime fechaFin)
    {
        var egresos = _context.Egresos
            .Where(e => e.Fecha >= fechaInicio && e.Fecha <= fechaFin)
            .OrderByDescending(e => e.Fecha)
            .ToList();

        reporte.CantidadEgresos = egresos.Count();
        reporte.TotalEgresos = egresos.Sum(e => e.Monto);

        reporte.ListaEgresos = egresos.Select(e => new EgresoReporteDto
        {
            Id = e.Id,
            Concepto = e.Descripcion,
            Fecha = e.Fecha,
            Monto = e.Monto,
            Categoria = e.Categoria,
            Descripcion = e.Observaciones ?? ""
        }).ToList();
    }

    private void ObtenerDatosClientes(ReporteMensualViewModel reporte, DateTime fechaInicio, DateTime fechaFin)
    {
        // Clientes nuevos del periodo
        var clientesNuevos = _context.Clientes
            .Include(c => c.ClienteServicios)
                .ThenInclude(cs => cs.Servicio)
            .Where(c => c.FechaCreacion >= fechaInicio && c.FechaCreacion <= fechaFin)
            .OrderByDescending(c => c.FechaCreacion)
            .ToList();

        reporte.ClientesNuevos = clientesNuevos.Count();

        // Clientes activos al final del periodo
        reporte.ClientesActivos = _context.Clientes
            .Count(c => c.Activo && c.FechaCreacion <= fechaFin);

        reporte.ListaClientesNuevos = clientesNuevos.Select(c => new ClienteNuevoDto
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Nombre = c.Nombre,
            FechaCreacion = c.FechaCreacion,
            Servicios = string.Join(", ", c.ClienteServicios
                .Where(cs => cs.Activo && cs.Servicio != null)
                .Select(cs => cs.Servicio!.Nombre))
        }).ToList();
    }

    private void CalcularComparacionMesAnterior(ReporteMensualViewModel reporte)
    {
        // Calcular el periodo anterior (mismo número de días)
        var diasPeriodo = (reporte.FechaFin - reporte.FechaInicio).Days + 1;
        var fechaInicioAnterior = reporte.FechaInicio.AddDays(-diasPeriodo);
        var fechaFinAnterior = reporte.FechaInicio.AddDays(-1);

        // Ingresos del periodo anterior
        var ingresosPeriodoAnterior = _context.Pagos
            .Where(p => p.FechaPago >= fechaInicioAnterior && p.FechaPago <= fechaFinAnterior)
            .Sum(p => (decimal?)p.Monto) ?? 0;

        // Egresos del periodo anterior
        var egresosPeriodoAnterior = _context.Egresos
            .Where(e => e.Fecha >= fechaInicioAnterior && e.Fecha <= fechaFinAnterior)
            .Sum(e => (decimal?)e.Monto) ?? 0;

        // Calcular diferencias
        reporte.DiferenciaIngresos = reporte.TotalIngresos - ingresosPeriodoAnterior;
        reporte.DiferenciaEgresos = reporte.TotalEgresos - egresosPeriodoAnterior;

        // Calcular porcentajes de variación
        if (ingresosPeriodoAnterior > 0)
        {
            reporte.PorcentajeVariacionIngresos = (reporte.DiferenciaIngresos / ingresosPeriodoAnterior) * 100;
        }

        if (egresosPeriodoAnterior > 0)
        {
            reporte.PorcentajeVariacionEgresos = (reporte.DiferenciaEgresos / egresosPeriodoAnterior) * 100;
        }
    }

    private string ObtenerTextoPeriodo(DateTime fechaInicio, DateTime fechaFin)
    {
        // Si es un mes completo
        if (fechaInicio.Day == 1 && fechaFin.Day == DateTime.DaysInMonth(fechaFin.Year, fechaFin.Month))
        {
            return fechaInicio.ToString("MMMM yyyy", _cultura).ToUpper();
        }

        // Si no, mostrar rango
        return $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}";
    }

    public byte[] ExportarReporteExcel(ReporteMensualViewModel reporte)
    {
        using var workbook = new XLWorkbook();
        
        // Hoja 1: Resumen Ejecutivo
        var wsResumen = workbook.Worksheets.Add("Resumen Ejecutivo");
        CrearHojaResumenExcel(wsResumen, reporte);
        
        // Hoja 2: Facturas
        var wsFacturas = workbook.Worksheets.Add("Facturas");
        CrearHojaFacturasExcel(wsFacturas, reporte);
        
        // Hoja 3: Pagos
        var wsPagos = workbook.Worksheets.Add("Pagos");
        CrearHojaPagosExcel(wsPagos, reporte);
        
        // Hoja 4: Egresos
        var wsEgresos = workbook.Worksheets.Add("Egresos");
        CrearHojaEgresosExcel(wsEgresos, reporte);
        
        // Hoja 5: Clientes Nuevos
        var wsClientes = workbook.Worksheets.Add("Clientes Nuevos");
        CrearHojaClientesExcel(wsClientes, reporte);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CrearHojaResumenExcel(IXLWorksheet ws, ReporteMensualViewModel reporte)
    {
        int row = 1;
        
        // Título
        ws.Cell(row, 1).Value = "REPORTE MENSUAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;
        
        // Periodo
        ws.Cell(row, 1).Value = "Periodo:";
        ws.Cell(row, 2).Value = reporte.PeriodoTexto;
        ws.Cell(row, 1).Style.Font.Bold = true;
        row += 2;
        
        // Resumen Ejecutivo
        ws.Cell(row, 1).Value = "RESUMEN EJECUTIVO";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
        row++;
        
        ws.Cell(row, 1).Value = "Total Ingresos:";
        ws.Cell(row, 2).Value = reporte.TotalIngresos;
        ws.Cell(row, 2).Style.NumberFormat.Format = "C$ #,##0.00";
        row++;
        
        ws.Cell(row, 1).Value = "Total Egresos:";
        ws.Cell(row, 2).Value = reporte.TotalEgresos;
        ws.Cell(row, 2).Style.NumberFormat.Format = "C$ #,##0.00";
        row++;
        
        ws.Cell(row, 1).Value = "Balance:";
        ws.Cell(row, 2).Value = reporte.Balance;
        ws.Cell(row, 2).Style.NumberFormat.Format = "C$ #,##0.00";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.Bold = true;
        row += 2;
        
        // Ingresos por Categoría
        ws.Cell(row, 1).Value = "INGRESOS POR CATEGORÍA";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        row++;
        
        ws.Cell(row, 1).Value = "Internet:";
        ws.Cell(row, 2).Value = reporte.IngresosInternet;
        ws.Cell(row, 2).Style.NumberFormat.Format = "C$ #,##0.00";
        row++;
        
        ws.Cell(row, 1).Value = "Streaming:";
        ws.Cell(row, 2).Value = reporte.IngresosStreaming;
        ws.Cell(row, 2).Style.NumberFormat.Format = "C$ #,##0.00";
        row += 2;
        
        // Facturas
        ws.Cell(row, 1).Value = "FACTURAS";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightYellow;
        row++;
        
        ws.Cell(row, 1).Value = "Facturas Generadas:";
        ws.Cell(row, 2).Value = reporte.FacturasGeneradas;
        row++;
        
        ws.Cell(row, 1).Value = "Facturas Pagadas:";
        ws.Cell(row, 2).Value = reporte.FacturasPagadas;
        row++;
        
        ws.Cell(row, 1).Value = "Facturas Pendientes:";
        ws.Cell(row, 2).Value = reporte.FacturasPendientes;
        row++;
        
        ws.Cell(row, 1).Value = "Internet:";
        ws.Cell(row, 2).Value = reporte.FacturasInternet;
        row++;
        
        ws.Cell(row, 1).Value = "Streaming:";
        ws.Cell(row, 2).Value = reporte.FacturasStreaming;
        row += 2;
        
        // Clientes
        ws.Cell(row, 1).Value = "CLIENTES";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightCyan;
        row++;
        
        ws.Cell(row, 1).Value = "Clientes Nuevos:";
        ws.Cell(row, 2).Value = reporte.ClientesNuevos;
        row++;
        
        ws.Cell(row, 1).Value = "Clientes Activos:";
        ws.Cell(row, 2).Value = reporte.ClientesActivos;
        
        // Ajustar columnas
        ws.Columns().AdjustToContents();
    }

    private void CrearHojaFacturasExcel(IXLWorksheet ws, ReporteMensualViewModel reporte)
    {
        // Encabezados
        ws.Cell(1, 1).Value = "Número";
        ws.Cell(1, 2).Value = "Cliente Código";
        ws.Cell(1, 3).Value = "Cliente Nombre";
        ws.Cell(1, 4).Value = "Fecha";
        ws.Cell(1, 5).Value = "Monto";
        ws.Cell(1, 6).Value = "Estado";
        ws.Cell(1, 7).Value = "Categoría";
        
        ws.Range(1, 1, 1, 7).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Datos
        int row = 2;
        foreach (var factura in reporte.ListaFacturas)
        {
            ws.Cell(row, 1).Value = factura.Numero;
            ws.Cell(row, 2).Value = factura.ClienteCodigo;
            ws.Cell(row, 3).Value = factura.ClienteNombre;
            ws.Cell(row, 4).Value = factura.Fecha;
            ws.Cell(row, 4).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 5).Value = factura.Monto;
            ws.Cell(row, 5).Style.NumberFormat.Format = "C$ #,##0.00";
            ws.Cell(row, 6).Value = factura.Estado;
            ws.Cell(row, 7).Value = factura.Categoria;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }

    private void CrearHojaPagosExcel(IXLWorksheet ws, ReporteMensualViewModel reporte)
    {
        // Encabezados
        ws.Cell(1, 1).Value = "Cliente Código";
        ws.Cell(1, 2).Value = "Cliente Nombre";
        ws.Cell(1, 3).Value = "Fecha";
        ws.Cell(1, 4).Value = "Monto";
        ws.Cell(1, 5).Value = "Método Pago";
        ws.Cell(1, 6).Value = "Referencia";
        
        ws.Range(1, 1, 1, 6).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Datos
        int row = 2;
        foreach (var pago in reporte.ListaPagos)
        {
            ws.Cell(row, 1).Value = pago.ClienteCodigo;
            ws.Cell(row, 2).Value = pago.ClienteNombre;
            ws.Cell(row, 3).Value = pago.Fecha;
            ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 4).Value = pago.Monto;
            ws.Cell(row, 4).Style.NumberFormat.Format = "C$ #,##0.00";
            ws.Cell(row, 5).Value = pago.MetodoPago;
            ws.Cell(row, 6).Value = pago.NumeroReferencia;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }

    private void CrearHojaEgresosExcel(IXLWorksheet ws, ReporteMensualViewModel reporte)
    {
        // Encabezados
        ws.Cell(1, 1).Value = "Concepto";
        ws.Cell(1, 2).Value = "Fecha";
        ws.Cell(1, 3).Value = "Monto";
        ws.Cell(1, 4).Value = "Categoría";
        ws.Cell(1, 5).Value = "Descripción";
        
        ws.Range(1, 1, 1, 5).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Datos
        int row = 2;
        foreach (var egreso in reporte.ListaEgresos)
        {
            ws.Cell(row, 1).Value = egreso.Concepto;
            ws.Cell(row, 2).Value = egreso.Fecha;
            ws.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            ws.Cell(row, 3).Value = egreso.Monto;
            ws.Cell(row, 3).Style.NumberFormat.Format = "C$ #,##0.00";
            ws.Cell(row, 4).Value = egreso.Categoria;
            ws.Cell(row, 5).Value = egreso.Descripcion;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }

    private void CrearHojaClientesExcel(IXLWorksheet ws, ReporteMensualViewModel reporte)
    {
        // Encabezados
        ws.Cell(1, 1).Value = "Código";
        ws.Cell(1, 2).Value = "Nombre";
        ws.Cell(1, 3).Value = "Fecha Creación";
        ws.Cell(1, 4).Value = "Servicios";
        
        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.LightGray;
        
        // Datos
        int row = 2;
        foreach (var cliente in reporte.ListaClientesNuevos)
        {
            ws.Cell(row, 1).Value = cliente.Codigo;
            ws.Cell(row, 2).Value = cliente.Nombre;
            ws.Cell(row, 3).Value = cliente.FechaCreacion;
            ws.Cell(row, 3).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(row, 4).Value = cliente.Servicios;
            row++;
        }
        
        ws.Columns().AdjustToContents();
    }

    public byte[] ExportarReportePdf(ReporteMensualViewModel reporte)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Height(80)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(10)
                    .Column(column =>
                    {
                        column.Item().Text("REPORTE MENSUAL")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.White);
                        column.Item().Text(reporte.PeriodoTexto)
                            .FontSize(14)
                            .FontColor(Colors.White);
                    });

                page.Content()
                    .PaddingVertical(10)
                    .Column(column =>
                    {
                        // Resumen Ejecutivo
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Background(Colors.Green.Lighten3).Padding(5)
                                    .Text("RESUMEN EJECUTIVO").Bold().FontSize(12);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Total Ingresos: ").Bold();
                                        r.AutoItem().Text($"C$ {reporte.TotalIngresos.ToString("N2", _cultura)}");
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Total Egresos: ").Bold();
                                        r.AutoItem().Text($"C$ {reporte.TotalEgresos.ToString("N2", _cultura)}");
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Balance: ").Bold();
                                        r.AutoItem().Text($"C$ {reporte.Balance.ToString("N2", _cultura)}")
                                            .FontColor(reporte.Balance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2)
                                            .Bold();
                                    });
                                });
                            });
                        });

                        // Ingresos por Categoría
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem(1).Padding(2).Column(col =>
                            {
                                col.Item().Background(Colors.Blue.Lighten3).Padding(5)
                                    .Text("INTERNET").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Text($"Ingresos: C$ {reporte.IngresosInternet.ToString("N2", _cultura)}");
                                    inner.Item().Text($"Facturas: {reporte.FacturasInternet}");
                                    inner.Item().Text($"Pendientes: C$ {reporte.PendientesInternet.ToString("N2", _cultura)}");
                                });
                            });

                            row.RelativeItem(1).Padding(2).Column(col =>
                            {
                                col.Item().Background(Colors.Purple.Lighten3).Padding(5)
                                    .Text("STREAMING").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Text($"Ingresos: C$ {reporte.IngresosStreaming.ToString("N2", _cultura)}");
                                    inner.Item().Text($"Facturas: {reporte.FacturasStreaming}");
                                    inner.Item().Text($"Pendientes: C$ {reporte.PendientesStreaming.ToString("N2", _cultura)}");
                                });
                            });
                        });

                        // Estadísticas
                        column.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem(1).Padding(2).Column(col =>
                            {
                                col.Item().Background(Colors.Orange.Lighten3).Padding(5)
                                    .Text("FACTURAS").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Text($"Generadas: {reporte.FacturasGeneradas}");
                                    inner.Item().Text($"Pagadas: {reporte.FacturasPagadas}");
                                    inner.Item().Text($"Pendientes: {reporte.FacturasPendientes}");
                                });
                            });

                            row.RelativeItem(1).Padding(2).Column(col =>
                            {
                                col.Item().Background(Colors.Teal.Lighten3).Padding(5)
                                    .Text("PAGOS").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Text($"Recibidos: {reporte.PagosRecibidos}");
                                    inner.Item().Text($"Monto: C$ {reporte.MontoPagos.ToString("N2", _cultura)}");
                                });
                            });

                            row.RelativeItem(1).Padding(2).Column(col =>
                            {
                                col.Item().Background(Colors.Cyan.Lighten3).Padding(5)
                                    .Text("CLIENTES").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Text($"Nuevos: {reporte.ClientesNuevos}");
                                    inner.Item().Text($"Activos: {reporte.ClientesActivos}");
                                });
                            });
                        });

                        // Comparación con periodo anterior
                        if (reporte.PorcentajeVariacionIngresos != 0 || reporte.PorcentajeVariacionEgresos != 0)
                        {
                            column.Item().PaddingBottom(10).Column(col =>
                            {
                                col.Item().Background(Colors.Grey.Lighten3).Padding(5)
                                    .Text("COMPARACIÓN CON PERIODO ANTERIOR").Bold().FontSize(10);
                                col.Item().Padding(5).Column(inner =>
                                {
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Variación Ingresos: ");
                                        var simbolo = reporte.PorcentajeVariacionIngresos > 0 ? "+" : "";
                                        r.AutoItem().Text($"{simbolo}{reporte.PorcentajeVariacionIngresos:F1}%")
                                            .FontColor(reporte.PorcentajeVariacionIngresos >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                    });
                                    inner.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("Variación Egresos: ");
                                        var simbolo = reporte.PorcentajeVariacionEgresos > 0 ? "+" : "";
                                        r.AutoItem().Text($"{simbolo}{reporte.PorcentajeVariacionEgresos:F1}%")
                                            .FontColor(reporte.PorcentajeVariacionEgresos >= 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                                    });
                                });
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8))
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                        x.Span($" | Generado: {DateTime.Now:dd/MM/yyyy HH:mm}");
                    });
                });
            }).GeneratePdf();
    }
}



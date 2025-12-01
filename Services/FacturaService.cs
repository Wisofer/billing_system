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
        var factura = _context.Facturas
            .Include(f => f.Cliente)
                .ThenInclude(c => c.ClienteServicios)
            .Include(f => f.Servicio)
            .Include(f => f.FacturaServicios)
                .ThenInclude(fs => fs.Servicio)
            .FirstOrDefault(f => f.Id == id);
        
        if (factura != null)
        {
            // Cargar pagos relacionados: directos (FacturaId) e indirectos (a través de PagoFacturas)
            var pagosDirectos = _context.Pagos
                .Where(p => p.FacturaId == factura.Id)
                .ToList();
            
            var pagosIndirectos = _context.PagoFacturas
                .Where(pf => pf.FacturaId == factura.Id)
                .Include(pf => pf.Pago)
                .Select(pf => pf.Pago)
                .ToList();
            
            // Combinar ambos tipos de pagos y eliminar duplicados
            var todosLosPagos = pagosDirectos
                .Union(pagosIndirectos)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderBy(p => p.FechaPago)
                .ToList();
            
            // Asignar los pagos a la factura
            factura.Pagos = todosLosPagos;
        }
        
        return factura;
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

    public string GenerarNumeroFactura(Cliente cliente, DateTime mes, string? categoria = null)
    {
        var mesStr = mes.ToString("MM");
        var añoStr = mes.ToString("yyyy");
        
        // Obtener el último número de factura para este mes y año de la misma categoría
        var ultimoNumero = _context.Facturas
            .Where(f => f.MesFacturacion.Year == mes.Year && 
                       f.MesFacturacion.Month == mes.Month &&
                       (categoria == null || f.Categoria == categoria))
            .OrderByDescending(f => f.Id)
            .Select(f => f.Numero)
            .FirstOrDefault();
        
        int numero = 1;
        if (!string.IsNullOrEmpty(ultimoNumero))
        {
            // Extraer el número del formato "XXXX-Nombre-MMYYYY" o "XXXX-Nombre-MMYYYY-STR"
            var partes = ultimoNumero.Split('-');
            if (partes.Length > 0 && int.TryParse(partes[0], out var num))
            {
                numero = num + 1;
            }
        }
        
        var nombreSinEspacios = cliente.Nombre.Replace(" ", "");
        var longitudNombre = Math.Min(10, nombreSinEspacios.Length);
        var nombreCliente = longitudNombre > 0 ? nombreSinEspacios.Substring(0, longitudNombre) : "Cliente";
        var sufijo = categoria == SD.CategoriaStreaming ? "-STR" : "";
        return $"{numero:D4}-{nombreCliente}-{mesStr}{añoStr}{sufijo}";
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

        factura.Numero = GenerarNumeroFactura(cliente, factura.MesFacturacion, servicio.Categoria);
        
        // Calcular monto: Streaming sin proporcional, Internet con proporcional
        if (servicio.Categoria == SD.CategoriaStreaming)
        {
            // Streaming: siempre precio completo (precio * cantidad)
            var clienteServicio = _context.ClienteServicios
                .FirstOrDefault(cs => cs.ClienteId == cliente.Id && cs.ServicioId == servicio.Id && cs.Activo);
            var cantidad = clienteServicio?.Cantidad ?? 1;
            factura.Monto = servicio.Precio * cantidad;
        }
        else
        {
            // Internet: aplicar proporcional
            factura.Monto = CalcularMontoProporcional(cliente, servicio, factura.MesFacturacion);
        }
        
        factura.Estado = SD.EstadoFacturaPendiente;
        factura.FechaCreacion = DateTime.Now;

        _context.Facturas.Add(factura);
        
        // Actualizar el último servicio usado del cliente
        cliente.ServicioId = factura.ServicioId;
        
        // NO incrementar TotalFacturas aquí - solo se incrementa cuando la factura se marca como Pagada
        
        _context.SaveChanges();
        return factura;
    }

    public List<Factura> CrearFacturasAgrupadasPorCategoria(int clienteId, List<int> servicioIds, DateTime mesFacturacion)
    {
        var cliente = _clienteService.ObtenerPorId(clienteId);
        if (cliente == null)
            throw new Exception("Cliente no encontrado");

        var facturasCreadas = new List<Factura>();

        // Obtener servicios y agrupar por categoría
        // IMPORTANTE: Filtrar servicios válidos y evitar duplicados
        var serviciosConInfo = servicioIds
            .Select(id => new
            {
                ServicioId = id,
                Servicio = _servicioService.ObtenerPorId(id),
                ClienteServicio = _context.ClienteServicios
                    .FirstOrDefault(cs => cs.ClienteId == clienteId && cs.ServicioId == id && cs.Activo)
            })
            .Where(x => x.Servicio != null && 
                       x.Servicio.Activo && 
                       !string.IsNullOrWhiteSpace(x.Servicio.Categoria))
            .GroupBy(x => x.ServicioId) // Agrupar por ServicioId para evitar duplicados
            .Select(g => g.First()) // Tomar el primero si hay duplicados
            .GroupBy(x => x.Servicio!.Categoria) // Luego agrupar por categoría
            .ToList();

        foreach (var grupoCategoria in serviciosConInfo)
        {
            var categoria = grupoCategoria.Key;
            var serviciosDelGrupo = grupoCategoria.ToList();

            // Validar que la categoría sea válida
            if (categoria != SD.CategoriaInternet && categoria != SD.CategoriaStreaming)
            {
                continue; // Saltar categorías inválidas
            }

            // Verificar si ya existe factura para esta categoría en este mes
            var existeFactura = _context.Facturas.Any(f =>
                f.ClienteId == clienteId &&
                f.Categoria == categoria &&
                f.MesFacturacion.Year == mesFacturacion.Year &&
                f.MesFacturacion.Month == mesFacturacion.Month);

            if (existeFactura)
            {
                continue; // Ya existe factura para esta categoría
            }

            // Calcular montos y crear FacturaServicios
            decimal montoTotal = 0;
            var primerServicio = serviciosDelGrupo.First().Servicio!;
            var facturaServicios = new List<FacturaServicio>();

            foreach (var item in serviciosDelGrupo)
            {
                var servicio = item.Servicio!;
                var clienteServicio = item.ClienteServicio;
                int cantidad = 1;
                
                // Para Streaming, obtener la cantidad de suscripciones
                if (servicio.Categoria == SD.CategoriaStreaming && clienteServicio != null)
                {
                    cantidad = clienteServicio.Cantidad;
                }
                
                // Para Streaming: precio completo sin proporcional
                // Para Internet: aplicar proporcional si aplica
                decimal montoServicio;
                
                if (servicio.Categoria == SD.CategoriaStreaming)
                {
                    // Streaming: siempre precio completo (precio * cantidad)
                    montoServicio = servicio.Precio * cantidad;
                }
                else
                {
                    // Internet: aplicar proporcional
                    var precioTotalSinProporcional = servicio.Precio * cantidad;
                    var fechaInicio = clienteServicio?.FechaInicio ?? cliente.FechaCreacion;
                    var montoProporcionalUnitario = CalcularMontoProporcionalConFechaInicio(cliente, servicio, mesFacturacion, fechaInicio);
                    var factorProporcional = servicio.Precio > 0 ? montoProporcionalUnitario / servicio.Precio : 1;
                    montoServicio = precioTotalSinProporcional * factorProporcional;
                }

                montoTotal += montoServicio;

                // Crear FacturaServicio - guardar el monto total y la cantidad
                facturaServicios.Add(new FacturaServicio
                {
                    ServicioId = servicio.Id,
                    Cantidad = cantidad,
                    Monto = montoServicio
                });
            }

            // Crear la factura consolidada
            var factura = new Factura
            {
                ClienteId = clienteId,
                ServicioId = primerServicio.Id, // Servicio principal para compatibilidad
                Categoria = categoria,
                MesFacturacion = mesFacturacion,
                Numero = GenerarNumeroFactura(cliente, mesFacturacion, categoria),
                Monto = montoTotal,
                Estado = SD.EstadoFacturaPendiente,
                FechaCreacion = DateTime.Now,
                FacturaServicios = facturaServicios
            };

            _context.Facturas.Add(factura);
            facturasCreadas.Add(factura);
        }

        _context.SaveChanges();
        return facturasCreadas;
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

        // Verificar si tiene pagos (directos o a través de PagoFactura)
        var tienePagos = _context.Pagos.Any(p => p.FacturaId == id) ||
                        _context.PagoFacturas.Any(pf => pf.FacturaId == id);
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

            // Verificar si tiene pagos (directos o a través de PagoFactura)
            var tienePagos = _context.Pagos.Any(p => p.FacturaId == id) ||
                            _context.PagoFacturas.Any(pf => pf.FacturaId == id);
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
        var clientes = _clienteService.ObtenerTodos().Where(c => c.Activo).ToList();
        // CORRECCIÓN: Facturar el mes anterior (primero se consume, luego se paga)
        // Si estamos en diciembre, facturamos noviembre
        var mesFacturacion = DateTime.Now.AddMonths(-1);
        mesFacturacion = new DateTime(mesFacturacion.Year, mesFacturacion.Month, 1);

        if (!clientes.Any())
        {
            return; // No hay clientes activos
        }

        var facturasACrear = new List<Factura>();

        foreach (var cliente in clientes)
        {
            // Obtener todos los servicios activos del cliente
            var serviciosActivos = _clienteService.ObtenerServiciosActivos(cliente.Id);

            if (!serviciosActivos.Any())
            {
                continue; // Saltar clientes sin servicios activos
            }

            // Filtrar servicios válidos (con servicio activo y categoría válida)
            var serviciosValidos = serviciosActivos
                .Where(cs => cs.Servicio != null && 
                            cs.Servicio.Activo && 
                            !string.IsNullOrWhiteSpace(cs.Servicio.Categoria))
                .ToList();

            if (!serviciosValidos.Any())
            {
                continue; // Saltar si no hay servicios válidos
            }

            // IMPORTANTE: Agrupar por ServicioId primero para evitar duplicados
            // Si hay múltiples ClienteServicio con el mismo ServicioId, solo tomar uno
            var serviciosUnicos = serviciosValidos
                .GroupBy(cs => cs.ServicioId)
                .Select(g => g.OrderBy(cs => cs.FechaInicio).First()) // Tomar el más antiguo si hay duplicados
                .ToList();

            // Agrupar servicios únicos por categoría
            var serviciosPorCategoria = serviciosUnicos
                .GroupBy(cs => cs.Servicio!.Categoria)
                .ToList();

            foreach (var grupoCategoria in serviciosPorCategoria)
            {
                var categoria = grupoCategoria.Key;
                var serviciosDelGrupo = grupoCategoria.ToList();

                // Validar que la categoría sea válida
                if (categoria != SD.CategoriaInternet && categoria != SD.CategoriaStreaming)
                {
                    continue; // Saltar categorías inválidas
                }

                // Verificar si ya existe factura para este cliente y categoría en este mes
                var existeFactura = _context.Facturas.Any(f =>
                    f.ClienteId == cliente.Id &&
                    f.Categoria == categoria &&
                    f.MesFacturacion.Year == mesFacturacion.Year &&
                    f.MesFacturacion.Month == mesFacturacion.Month);

                if (existeFactura)
                {
                    continue; // Ya existe factura para esta categoría
                }

                // Calcular montos y crear FacturaServicios
                decimal montoTotal = 0;
                var primerServicio = serviciosDelGrupo.First().Servicio!;
                var facturaServicios = new List<FacturaServicio>();

                foreach (var clienteServicio in serviciosDelGrupo)
                {
                    var servicio = clienteServicio.Servicio!;
                    int cantidad = 1;
                    
                    // Para Streaming, obtener la cantidad de suscripciones
                    if (servicio.Categoria == SD.CategoriaStreaming)
                    {
                        cantidad = clienteServicio.Cantidad;
                    }
                    
                    // Para Streaming: precio completo sin proporcional
                    // Para Internet: aplicar proporcional si aplica
                    decimal montoServicio;
                    
                    if (servicio.Categoria == SD.CategoriaStreaming)
                    {
                        // Streaming: siempre precio completo (precio * cantidad)
                        montoServicio = servicio.Precio * cantidad;
                    }
                    else
                    {
                        // Internet: aplicar proporcional
                        var precioTotalSinProporcional = servicio.Precio * cantidad;
                        var montoProporcionalUnitario = CalcularMontoProporcionalConFechaInicio(cliente, servicio, mesFacturacion, clienteServicio.FechaInicio);
                        var factorProporcional = servicio.Precio > 0 ? montoProporcionalUnitario / servicio.Precio : 1;
                        montoServicio = precioTotalSinProporcional * factorProporcional;
                    }

                    montoTotal += montoServicio;

                    // Crear FacturaServicio - guardar el monto total y la cantidad
                    facturaServicios.Add(new FacturaServicio
                    {
                        ServicioId = servicio.Id,
                        Cantidad = cantidad,
                        Monto = montoServicio
                    });
                }

                // Crear la factura consolidada
                // Usar GenerarNumeroFactura para evitar duplicados y obtener el número correcto
                var numeroFactura = GenerarNumeroFactura(cliente, mesFacturacion, categoria);
                
                var factura = new Factura
                {
                    ClienteId = cliente.Id,
                    ServicioId = primerServicio.Id, // Servicio principal para compatibilidad
                    Categoria = categoria,
                    MesFacturacion = mesFacturacion,
                    Numero = numeroFactura,
                    Monto = montoTotal,
                    Estado = SD.EstadoFacturaPendiente,
                    FechaCreacion = DateTime.Now,
                    FacturaServicios = facturaServicios
                };

                facturasACrear.Add(factura);
            }
        }

        // Guardar todas las facturas en una sola transacción
        if (facturasACrear.Any())
        {
            _context.Facturas.AddRange(facturasACrear);
            _context.SaveChanges();
        }
    }

    /// <summary>
    /// Calcula el monto proporcional considerando la fecha de inicio del servicio específico.
    /// El proporcional se aplica solo si el cliente inició en el mes de facturación y después del día 5.
    /// El ciclo es del día 5 al día 5 de cada mes.
    /// </summary>
    private decimal CalcularMontoProporcionalConFechaInicio(Cliente cliente, Servicio servicio, DateTime mesFacturacion, DateTime fechaInicioServicio)
    {
        // Usar la fecha de inicio del servicio
        var fechaInicio = fechaInicioServicio.Date;
        var primerDiaMesFacturacion = new DateTime(mesFacturacion.Year, mesFacturacion.Month, 1);
        var ultimoDiaMesFacturacion = primerDiaMesFacturacion.AddMonths(1).AddDays(-1);
        
        // Si el cliente inició después del mes de facturación, no debe facturarse aún
        // (esto no debería pasar en la generación automática, pero es una validación de seguridad)
        if (fechaInicio > ultimoDiaMesFacturacion)
        {
            return servicio.Precio; // No debería facturarse, pero por seguridad retornamos precio completo
        }
        
        // Si el cliente inició antes del mes de facturación, ya pagó proporcional en su primera factura
        // Por lo tanto, debe pagar mes completo
        if (fechaInicio < primerDiaMesFacturacion)
        {
            return servicio.Precio;
        }
        
        // Si el cliente inició en el mes de facturación:
        // - Si inició el día 5 o antes, paga mes completo
        if (fechaInicio.Day <= 5)
        {
            return servicio.Precio;
        }
        
        // - Si inició después del día 5, calcular proporcional
        // El proporcional se calcula desde la fecha de inicio hasta el día 5 del mes siguiente
        var siguienteMes = mesFacturacion.AddMonths(1);
        var fechaVencimiento = new DateTime(siguienteMes.Year, siguienteMes.Month, 5);
        
        // Calcular días consumidos: desde fecha de inicio hasta día 5 del siguiente mes (incluyendo ambos días)
        var diasConsumidos = (fechaVencimiento.Date - fechaInicio.Date).Days + 1;
        
        if (diasConsumidos <= 0)
        {
            return servicio.Precio;
        }

        // Obtener días del mes de facturación (considera correctamente 28, 29, 30, 31 días)
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


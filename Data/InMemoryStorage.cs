using billing_system.Models.Entities;

namespace billing_system.Data;

public static class InMemoryStorage
{
    // Almacenamiento en memoria
    private static List<Cliente> _clientes = new();
    private static List<Factura> _facturas = new();
    private static List<Pago> _pagos = new();
    private static List<Servicio> _servicios = new();
    
    // Contadores para IDs
    private static int _clienteIdCounter = 1;
    private static int _facturaIdCounter = 1;
    private static int _pagoIdCounter = 1;
    private static int _servicioIdCounter = 1;

    // Propiedades públicas para acceso
    public static List<Cliente> Clientes => _clientes;
    public static List<Factura> Facturas => _facturas;
    public static List<Pago> Pagos => _pagos;
    public static List<Servicio> Servicios => _servicios;

    // Métodos para obtener IDs
    public static int GetNextClienteId() => _clienteIdCounter++;
    public static int GetNextFacturaId() => _facturaIdCounter++;
    public static int GetNextPagoId() => _pagoIdCounter++;
    public static int GetNextServicioId() => _servicioIdCounter++;

    // Inicializar datos
    public static void Initialize()
    {
        if (_servicios.Count == 0)
        {
            // Crear servicios principales
            _servicios.Add(new Servicio
            {
                Id = GetNextServicioId(),
                Nombre = "Servicio 1",
                Precio = 920m,
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            _servicios.Add(new Servicio
            {
                Id = GetNextServicioId(),
                Nombre = "Servicio 2",
                Precio = 1104m,
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            _servicios.Add(new Servicio
            {
                Id = GetNextServicioId(),
                Nombre = "Servicio 3",
                Precio = 1288m,
                Activo = true,
                FechaCreacion = DateTime.Now
            });

            _servicios.Add(new Servicio
            {
                Id = GetNextServicioId(),
                Nombre = "Especial",
                Precio = 1000m,
                Activo = true,
                FechaCreacion = DateTime.Now
            });
        }
    }
}


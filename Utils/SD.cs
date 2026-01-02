namespace billing_system.Utils;

public static class SD
{
    // Roles de usuario
    public const string RolAdministrador = "Administrador";
    public const string RolNormal = "Normal";
    public const string RolCaja = "Caja";
    public const string RolDemo = "Demo";

    // Estados de Factura
    public const string EstadoFacturaPendiente = "Pendiente";
    public const string EstadoFacturaPagada = "Pagada";
    public const string EstadoFacturaCancelada = "Cancelada";

    // Tipos de Pago
    public const string TipoPagoFisico = "Fisico";
    public const string TipoPagoElectronico = "Electronico";
    public const string TipoPagoMixto = "Mixto";

    // Monedas
    public const string MonedaCordoba = "C$";
    public const string MonedaDolar = "$";
    public const string MonedaAmbos = "Ambos";

    // Tipo de Cambio
    public const decimal TipoCambioDolar = 36.80m; // C$36.80 = $1 (Venta)
    public const decimal TipoCambioCompra = 36.32m; // C$36.32 = $1 (Compra - cuando cliente paga en $)

    // Bancos
    public const string BancoBanpro = "Banpro";
    public const string BancoLafise = "Lafise";
    public const string BancoBAC = "BAC";
    public const string BancoFicohsa = "Ficohsa";
    public const string BancoBDF = "BDF";

    // Tipos de Cuenta
    public const string TipoCuentaDolar = "Cuenta $";
    public const string TipoCuentaCordoba = "Cuenta C$";
    public const string TipoCuentaBilletera = "Billetera movil";

    // Categorías de Servicio
    public const string CategoriaInternet = "Internet";
    public const string CategoriaStreaming = "Streaming";

    // Estados de Equipo (Inventario)
    public const string EstadoEquipoDisponible = "Disponible";
    public const string EstadoEquipoEnUso = "En uso";
    public const string EstadoEquipoDanado = "Dañado";
    public const string EstadoEquipoEnReparacion = "En reparación";
    public const string EstadoEquipoRetirado = "Retirado";

    // Tipos de Movimiento de Inventario
    public const string TipoMovimientoEntrada = "Entrada";
    public const string TipoMovimientoSalida = "Salida";

    // Subtipos de Movimiento de Inventario
    public const string SubtipoMovimientoCompra = "Compra";
    public const string SubtipoMovimientoVenta = "Venta";
    public const string SubtipoMovimientoAsignacion = "Asignación";
    public const string SubtipoMovimientoDevolucion = "Devolución";
    public const string SubtipoMovimientoAjuste = "Ajuste";
    public const string SubtipoMovimientoDano = "Daño";
    public const string SubtipoMovimientoTransferencia = "Transferencia";

    // Estados de Asignación de Equipo
    public const string EstadoAsignacionActiva = "Activa";
    public const string EstadoAsignacionDevuelta = "Devuelta";
    public const string EstadoAsignacionPerdida = "Perdida";

    // Tipos de Mantenimiento
    public const string TipoMantenimientoPreventivo = "Preventivo";
    public const string TipoMantenimientoCorrectivo = "Correctivo";

    // Estados de Mantenimiento
    public const string EstadoMantenimientoProgramado = "Programado";
    public const string EstadoMantenimientoEnProceso = "En proceso";
    public const string EstadoMantenimientoCompletado = "Completado";
    public const string EstadoMantenimientoCancelado = "Cancelado";

    // Tipos de Ubicación
    public const string TipoUbicacionAlmacen = "Almacen";
    public const string TipoUbicacionCampo = "Campo";
    public const string TipoUbicacionReparacion = "Reparacion";

    // Servicios Principales
    public static class ServiciosPrincipales
    {
        public const string Servicio1 = "Servicio 1";
        public const string Servicio2 = "Servicio 2";
        public const string Servicio3 = "Servicio 3";
        public const string ServicioEspecial = "Especial";

        public const decimal PrecioServicio1 = 920m;
        public const decimal PrecioServicio2 = 1104m;
        public const decimal PrecioServicio3 = 1288m;
        public const decimal PrecioServicioEspecial = 1000m;
    }

    // Usuarios estáticos (temporal, hasta conectar BD)
    public static class UsuariosEstaticos
    {
        public static List<Models.Entities.Usuario> ObtenerUsuarios()
        {
            return new List<Models.Entities.Usuario>
            {
                new Models.Entities.Usuario
                {
                    Id = 1,
                    NombreUsuario = "admin",
                    Contrasena = "admin", // En producción debe estar hasheada
                    Rol = RolAdministrador,
                    NombreCompleto = "Administrador del Sistema",
                    Activo = true
                },
                new Models.Entities.Usuario
                {
                    Id = 2,
                    NombreUsuario = "usuario",
                    Contrasena = "usuario",
                    Rol = RolNormal,
                    NombreCompleto = "Usuario Normal",
                    Activo = true
                }
            };
        }
    }
}


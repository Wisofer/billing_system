namespace billing_system.Utils;

public static class SD
{
    // Roles de usuario
    public const string RolAdministrador = "Administrador";
    public const string RolNormal = "Normal";
    public const string RolCaja = "Caja";

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
    public const decimal TipoCambioDolar = 36.80m; // C$36.80 = $1

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


using Microsoft.EntityFrameworkCore;
using billing_system.Models.Entities;

namespace billing_system.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Factura> Facturas { get; set; }
    public DbSet<FacturaServicio> FacturaServicios { get; set; }
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<PagoFactura> PagoFacturas { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<ClienteServicio> ClienteServicios { get; set; }
    public DbSet<PlantillaMensajeWhatsApp> PlantillasMensajeWhatsApp { get; set; }
    public DbSet<Configuracion> Configuraciones { get; set; }
    
    // Landing Page
    public DbSet<MetodoPago> MetodosPago { get; set; }
    public DbSet<ServicioLandingPage> ServiciosLandingPage { get; set; }
    
    // Inventario
    public DbSet<Equipo> Equipos { get; set; }
    public DbSet<CategoriaEquipo> CategoriasEquipo { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
    public DbSet<Proveedor> Proveedores { get; set; }
    public DbSet<MovimientoInventario> MovimientosInventario { get; set; }
    public DbSet<EquipoMovimiento> EquipoMovimientos { get; set; }
    public DbSet<AsignacionEquipo> AsignacionesEquipo { get; set; }
    public DbSet<MantenimientoReparacion> MantenimientosReparaciones { get; set; }
    public DbSet<HistorialEstadoEquipo> HistorialEstadosEquipo { get; set; }
    
    // Egresos/Gastos
    public DbSet<Egreso> Egresos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Cliente
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Cedula).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.TotalFacturas).HasDefaultValue(0);
            entity.HasIndex(e => e.Codigo).IsUnique();
            
            // Relación opcional con Servicio (último servicio usado)
            entity.HasOne(e => e.Servicio)
                .WithMany()
                .HasForeignKey(e => e.ServicioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración de Servicio
        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50).HasDefaultValue("Internet");
        });

        // Configuración de Factura
        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Numero).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(50).HasDefaultValue("Internet");
            entity.Property(e => e.ArchivoPDF).HasMaxLength(500);
            
            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.Facturas)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Servicio)
                .WithMany(s => s.Facturas)
                .HasForeignKey(e => e.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de FacturaServicio
        modelBuilder.Entity<FacturaServicio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            
            entity.HasOne(e => e.Factura)
                .WithMany(f => f.FacturaServicios)
                .HasForeignKey(e => e.FacturaId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Servicio)
                .WithMany()
                .HasForeignKey(e => e.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Pago
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Moneda).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TipoPago).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Banco).HasMaxLength(100);
            entity.Property(e => e.TipoCuenta).HasMaxLength(100);
            entity.Property(e => e.MontoRecibido).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Vuelto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observaciones).HasMaxLength(500);
            
            // Campos para pago físico con múltiples monedas
            entity.Property(e => e.MontoCordobasFisico).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoDolaresFisico).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoRecibidoFisico).HasColumnType("decimal(18,2)");
            entity.Property(e => e.VueltoFisico).HasColumnType("decimal(18,2)");
            
            // Campos para pago electrónico con múltiples monedas
            entity.Property(e => e.MontoCordobasElectronico).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MontoDolaresElectronico).HasColumnType("decimal(18,2)");
            
            // Relación con Factura (opcional, para compatibilidad con pagos de una sola factura)
            entity.HasOne(e => e.Factura)
                .WithMany(f => f.Pagos)
                .HasForeignKey(e => e.FacturaId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false); // Permitir null para pagos con múltiples facturas
            
            // Relación con PagoFactura (para pagos con múltiples facturas)
            entity.HasMany(e => e.PagoFacturas)
                .WithOne(pf => pf.Pago)
                .HasForeignKey(pf => pf.PagoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de PagoFactura
        modelBuilder.Entity<PagoFactura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MontoAplicado).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.Pago)
                .WithMany(p => p.PagoFacturas)
                .HasForeignKey(e => e.PagoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Factura)
                .WithMany()
                .HasForeignKey(e => e.FacturaId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Índice compuesto para evitar duplicados
            entity.HasIndex(e => new { e.PagoId, e.FacturaId }).IsUnique();
        });

        // Configuración de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Contrasena).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);
            entity.Property(e => e.NombreCompleto).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.NombreUsuario).IsUnique();
        });

        // Configuración de ClienteServicio (relación muchos-a-muchos)
        modelBuilder.Entity<ClienteServicio>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            
            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.ClienteServicios)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Servicio)
                .WithMany(s => s.ClienteServicios)
                .HasForeignKey(e => e.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Índice compuesto para mejorar búsquedas
            entity.HasIndex(e => new { e.ClienteId, e.ServicioId });
        });

        // Configuración de PlantillaMensajeWhatsApp
        modelBuilder.Entity<PlantillaMensajeWhatsApp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Mensaje).IsRequired().HasColumnType("text");
            entity.Property(e => e.Activa).HasDefaultValue(true);
            entity.Property(e => e.EsDefault).HasDefaultValue(false);
        });

        // Configuración de Configuracion
        modelBuilder.Entity<Configuracion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Clave).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Valor).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.UsuarioActualizacion).HasMaxLength(200);
            entity.HasIndex(e => e.Clave).IsUnique(); // Clave única para evitar duplicados
        });

        // Configuración de MetodoPago (Landing Page)
        modelBuilder.Entity<MetodoPago>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreBanco).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icono).HasMaxLength(10);
            entity.Property(e => e.TipoCuenta).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Moneda).HasMaxLength(10);
            entity.Property(e => e.NumeroCuenta).HasMaxLength(100);
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Orden).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
        });

        // Configuración de ServicioLandingPage (Landing Page)
        modelBuilder.Entity<ServicioLandingPage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Velocidad).HasMaxLength(50);
            entity.Property(e => e.Etiqueta).HasMaxLength(100);
            entity.Property(e => e.ColorEtiqueta).HasMaxLength(50);
            entity.Property(e => e.Icono).HasMaxLength(10);
            entity.Property(e => e.Caracteristicas).HasColumnType("text");
            entity.Property(e => e.Mensaje).HasMaxLength(500);
            entity.Property(e => e.Orden).HasDefaultValue(0);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.Property(e => e.Destacado).HasDefaultValue(false);
        });

        // Configuración de Equipo
        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.NumeroSerie).HasMaxLength(100);
            entity.Property(e => e.Marca).HasMaxLength(100);
            entity.Property(e => e.Modelo).HasMaxLength(100);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50).HasDefaultValue("Disponible");
            entity.Property(e => e.Stock).HasDefaultValue(0);
            entity.Property(e => e.StockMinimo).HasDefaultValue(0);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.NumeroSerie).IsUnique().HasFilter("\"NumeroSerie\" IS NOT NULL AND \"NumeroSerie\" != ''");
            
            entity.HasOne(e => e.CategoriaEquipo)
                .WithMany(c => c.Equipos)
                .HasForeignKey(e => e.CategoriaEquipoId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Ubicacion)
                .WithMany(u => u.Equipos)
                .HasForeignKey(e => e.UbicacionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Proveedor)
                .WithMany(p => p.Equipos)
                .HasForeignKey(e => e.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración de CategoriaEquipo
        modelBuilder.Entity<CategoriaEquipo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // Configuración de Ubicacion
        modelBuilder.Entity<Ubicacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Direccion).HasMaxLength(500);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50).HasDefaultValue("Almacen");
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        // Configuración de Proveedor
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Contacto).HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Direccion).HasMaxLength(500);
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
        });

        // Configuración de MovimientoInventario
        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Subtipo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de EquipoMovimiento
        modelBuilder.Entity<EquipoMovimiento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(18,2)");
            
            entity.HasOne(e => e.MovimientoInventario)
                .WithMany(m => m.EquipoMovimientos)
                .HasForeignKey(e => e.MovimientoInventarioId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Equipo)
                .WithMany()
                .HasForeignKey(e => e.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.UbicacionOrigen)
                .WithMany()
                .HasForeignKey(e => e.UbicacionOrigenId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.UbicacionDestino)
                .WithMany()
                .HasForeignKey(e => e.UbicacionDestinoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración de AsignacionEquipo
        modelBuilder.Entity<AsignacionEquipo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.EmpleadoNombre).HasMaxLength(200);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50).HasDefaultValue("Activa");
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            
            entity.HasOne(e => e.Equipo)
                .WithMany(eq => eq.AsignacionesEquipo)
                .HasForeignKey(e => e.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.AsignacionesEquipo)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuración de MantenimientoReparacion
        modelBuilder.Entity<MantenimientoReparacion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tipo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProveedorTecnico).HasMaxLength(200);
            entity.Property(e => e.Costo).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ProblemaReportado).HasMaxLength(1000);
            entity.Property(e => e.SolucionAplicada).HasMaxLength(1000);
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50).HasDefaultValue("Programado");
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            
            entity.HasOne(e => e.Equipo)
                .WithMany(eq => eq.MantenimientosReparaciones)
                .HasForeignKey(e => e.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de HistorialEstadoEquipo
        modelBuilder.Entity<HistorialEstadoEquipo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EstadoAnterior).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EstadoNuevo).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Motivo).HasMaxLength(500);
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            
            entity.HasOne(e => e.Equipo)
                .WithMany(eq => eq.HistorialEstados)
                .HasForeignKey(e => e.EquipoId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Egreso
        modelBuilder.Entity<Egreso>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Categoria).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.NumeroFactura).HasMaxLength(100);
            entity.Property(e => e.Proveedor).HasMaxLength(200);
            entity.Property(e => e.MetodoPago).HasMaxLength(50).HasDefaultValue("Efectivo");
            entity.Property(e => e.Observaciones).HasMaxLength(1000);
            entity.Property(e => e.Activo).HasDefaultValue(true);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Fecha);
            entity.HasIndex(e => e.Categoria);
            
            entity.HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}


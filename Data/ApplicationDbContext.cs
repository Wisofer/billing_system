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
    public DbSet<Pago> Pagos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }

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
            entity.Property(e => e.Precio).HasColumnType("decimal(18,2)");
        });

        // Configuración de Factura
        modelBuilder.Entity<Factura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Numero).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Monto).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Estado).IsRequired().HasMaxLength(50);
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
            
            entity.HasOne(e => e.Factura)
                .WithMany(f => f.Pagos)
                .HasForeignKey(e => e.FacturaId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }
}


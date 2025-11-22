using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Configurar URLs en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Configurar Entity Framework con MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    try
    {
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
    catch
    {
        // Si no puede detectar la versión, usar una versión específica
        options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)));
    }
});

// Configurar sesiones
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Registrar servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

var app = builder.Build();

// Aplicar migraciones e inicializar datos
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Aplicar migraciones
        dbContext.Database.Migrate();
        
        // Inicializar servicios si no existen
        if (!dbContext.Servicios.Any())
        {
            logger.LogInformation("Inicializando servicios en la base de datos...");
            
            var servicios = new List<Servicio>
            {
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio1, Precio = SD.ServiciosPrincipales.PrecioServicio1, Activo = true, FechaCreacion = DateTime.Now },
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio2, Precio = SD.ServiciosPrincipales.PrecioServicio2, Activo = true, FechaCreacion = DateTime.Now },
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio3, Precio = SD.ServiciosPrincipales.PrecioServicio3, Activo = true, FechaCreacion = DateTime.Now },
                new Servicio { Nombre = SD.ServiciosPrincipales.ServicioEspecial, Precio = SD.ServiciosPrincipales.PrecioServicioEspecial, Activo = true, FechaCreacion = DateTime.Now }
            };
            
            dbContext.Servicios.AddRange(servicios);
            dbContext.SaveChanges();
            logger.LogInformation("Servicios inicializados correctamente.");
        }

        // Migrar datos de la tabla antigua 'clientes' a 'Clientes'
        MigrateClientesData.MigrateOldClientesToNew(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // En desarrollo, solo usar HTTP si no hay HTTPS configurado
    // Esto evita la advertencia de redirección HTTPS
}

app.UseStaticFiles();

app.UseRouting();

// Habilitar sesiones
app.UseSession();


// Configurar rutas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

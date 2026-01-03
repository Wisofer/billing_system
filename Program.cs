using billing_system.Data;
using billing_system.Models.Entities;
using billing_system.Services;
using billing_system.Services.IServices;
using billing_system.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;
using System.Text;
using System.Globalization;

// Inicializar QuestPDF antes de cualquier uso
QuestPDF.Settings.License = LicenseType.Community;

// Registrar fuentes con soporte de emojis
try
{
    // Intentar registrar fuentes del sistema que soporten emojis
    var fontPaths = new[]
    {
        // Linux - Noto Color Emoji (común en sistemas Linux)
        "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf",
        "/usr/share/fonts/truetype/noto/NotoEmoji-Regular.ttf",
        "/usr/share/fonts/opentype/noto/NotoColorEmoji.ttf",
        // Windows - Segoe UI Emoji
        @"C:\Windows\Fonts\seguiemj.ttf",
        // macOS - Apple Color Emoji (aunque no es TTF, intentamos)
        "/System/Library/Fonts/Apple Color Emoji.ttc",
        // Ruta alternativa común
        "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf" // Fallback
    };

    bool fontRegistered = false;
    
    foreach (var fontPath in fontPaths)
    {
        try
        {
            if (File.Exists(fontPath))
            {
                using var fontStream = File.OpenRead(fontPath);
                // Registrar con un nombre personalizado para fácil acceso
                FontManager.RegisterFont(fontStream);
                Console.WriteLine($"Fuente con emojis registrada: {fontPath}");
                fontRegistered = true;
                break;
            }
        }
        catch
        {
            // Continuar con la siguiente fuente si esta falla
            continue;
        }
    }

    // Si no se encontró ninguna fuente del sistema, intentar usar una fuente embebida
    if (!fontRegistered)
    {
        // Intentar cargar desde Resources/Fonts si existe
        var resourceFontPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "NotoColorEmoji.ttf");
        if (File.Exists(resourceFontPath))
        {
            using var fontStream = File.OpenRead(resourceFontPath);
            FontManager.RegisterFont(fontStream);
            Console.WriteLine($"Fuente con emojis registrada desde recursos: {resourceFontPath}");
            fontRegistered = true;
        }
    }
    
    // Si aún no se registró, intentar usar una fuente que combine texto y emojis
    // Usaremos una fuente que tenga mejor soporte Unicode
    if (!fontRegistered)
    {
        // Intentar usar DejaVu Sans que tiene mejor soporte Unicode
        var dejaVuPaths = new[]
        {
            "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf"
        };
        
        foreach (var dejaVuPath in dejaVuPaths)
        {
            try
            {
                if (File.Exists(dejaVuPath))
                {
                    using var fontStream = File.OpenRead(dejaVuPath);
                    FontManager.RegisterFont(fontStream);
                    Console.WriteLine($"Fuente Unicode registrada como fallback: {dejaVuPath}");
                    break;
                }
            }
            catch { continue; }
        }
    }

    if (!fontRegistered)
    {
        Console.WriteLine("Advertencia: No se encontró una fuente con soporte de emojis. Los emojis pueden no renderizarse correctamente.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Advertencia al registrar fuentes con emojis: {ex.Message}");
    // Continuar sin la fuente de emojis - la aplicación seguirá funcionando
}

// Configurar Npgsql para manejar DateTime correctamente con PostgreSQL
// Esto permite usar DateTime.Now sin especificar UTC explícitamente
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configurar cultura predeterminada para Nicaragua (formato de números: coma para miles)
var culturaNicaragua = new CultureInfo("es-NI");
CultureInfo.DefaultThreadCurrentCulture = culturaNicaragua;
CultureInfo.DefaultThreadCurrentUICulture = culturaNicaragua;

var builder = WebApplication.CreateBuilder(args);

// Configurar CORS para permitir acceso desde React (Landing Page)
builder.Services.AddCors(options =>
{
    options.AddPolicy("LandingPagePolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",           // React Dev (Vite)
                "http://localhost:3000",           // React Dev (Create React App)
                "https://emsinetsolut.com",        // Producción
                "https://www.emsinetsolut.com",    // Producción www
                "https://landing.emsinetsolut.com" // Subdomain si lo usas
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        // Aceptar nombres de propiedades en minúsculas (case-insensitive)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Permitir leer números desde strings si es necesario (pero preferir números)
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

// Configurar URLs en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

// Configurar Entity Framework con PostgreSQL (Neon)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Configurar sesiones
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4); // 4 horas para operaciones largas
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configuración JWT para API Móvil
var jwtKey = builder.Configuration["Jwt:Key"] ?? "EmsInetSolutSuperSecretKeyForJwtAuthentication2024!@#$%";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EmsInetSolut";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EmsInetMovilApp";

builder.Services.AddSingleton(new JwtSettings
{
    Key = jwtKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpirationHours = 24 * 7 // 7 días
});

// Configurar Authentication con Cookies (Web) + JWT (Móvil)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies"; // Por defecto usa cookies (sistema web)
    options.DefaultChallengeScheme = "Cookies";
})
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(4); // 4 horas para operaciones largas (pagos, facturas, etc.)
        options.SlidingExpiration = true; // Renueva automáticamente con cada actividad del servidor
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
        };
    });

// Configurar Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireClaim("Rol", "Administrador"));
    options.AddPolicy("Normal", policy => policy.RequireClaim("Rol", "Normal", "Administrador"));
    options.AddPolicy("Caja", policy => policy.RequireClaim("Rol", "Caja", "Administrador"));
    options.AddPolicy("FacturasPagos", policy => policy.RequireClaim("Rol", "Normal", "Administrador", "Demo"));
    options.AddPolicy("Pagos", policy => policy.RequireClaim("Rol", "Caja", "Normal", "Administrador", "Demo"));
    options.AddPolicy("Inventario", policy => policy.RequireClaim("Rol", "Normal", "Administrador", "Demo"));
    options.AddPolicy("Demo", policy => policy.RequireClaim("Rol", "Demo", "Administrador"));
});

// Registrar servicios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IFacturaService, FacturaService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IPdfService>(sp => 
{
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var context = sp.GetRequiredService<ApplicationDbContext>();
    return new PdfService(environment, context);
});
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IConfiguracionService, ConfiguracionService>();

// Servicios de Inventario
builder.Services.AddScoped<IEquipoService, EquipoService>();
builder.Services.AddScoped<ICategoriaEquipoService, CategoriaEquipoService>();
builder.Services.AddScoped<IUbicacionService, UbicacionService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();
builder.Services.AddScoped<IAsignacionEquipoService, AsignacionEquipoService>();
builder.Services.AddScoped<IMantenimientoReparacionService, MantenimientoReparacionService>();
builder.Services.AddScoped<IMaterialInstalacionService, MaterialInstalacionService>();

// Servicios de Landing Page
builder.Services.AddScoped<IMetodoPagoService, MetodoPagoService>();
builder.Services.AddScoped<IServicioLandingPageService, ServicioLandingPageService>();

// Servicios de Egresos/Gastos
builder.Services.AddScoped<IEgresoService, EgresoService>();

// Servicios de Contacto (Landing Page)
builder.Services.AddScoped<IContactoService, ContactoService>();

// Servicios de Reportes
builder.Services.AddScoped<IReporteService, ReporteService>();

// Registrar servicio en segundo plano para generación automática de facturas
// Este servicio se ejecutará el día 1 de cada mes a las 2:00 AM
builder.Services.AddHostedService<FacturaAutomaticaBackgroundService>();

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
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio1, Precio = SD.ServiciosPrincipales.PrecioServicio1, Categoria = SD.CategoriaInternet, Activo = true, FechaCreacion = DateTime.UtcNow },
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio2, Precio = SD.ServiciosPrincipales.PrecioServicio2, Categoria = SD.CategoriaInternet, Activo = true, FechaCreacion = DateTime.UtcNow },
                new Servicio { Nombre = SD.ServiciosPrincipales.Servicio3, Precio = SD.ServiciosPrincipales.PrecioServicio3, Categoria = SD.CategoriaInternet, Activo = true, FechaCreacion = DateTime.UtcNow },
                new Servicio { Nombre = SD.ServiciosPrincipales.ServicioEspecial, Precio = SD.ServiciosPrincipales.PrecioServicioEspecial, Categoria = SD.CategoriaInternet, Activo = true, FechaCreacion = DateTime.UtcNow }
            };
            
            dbContext.Servicios.AddRange(servicios);
            dbContext.SaveChanges();
            logger.LogInformation("Servicios inicializados correctamente.");
        }

        // Migrar datos de la tabla antigua 'clientes' a 'Clientes'
        MigrateClientesData.MigrateOldClientesToNew(dbContext, logger);

        // Migrar ServicioId existentes a ClienteServicios
        MigrateClienteServicios.MigrateServiciosToClienteServicios(dbContext, logger);

        // Crear usuario admin si no existe
        InicializarUsuarioAdmin.CrearAdminSiNoExiste(dbContext, logger);
        InicializarUsuarioDemo.CrearDemoSiNoExiste(dbContext, logger);

        // Crear plantilla por defecto de WhatsApp si no existe
        InicializarPlantillaWhatsApp.CrearPlantillaDefaultSiNoExiste(dbContext, logger);

        // Inicializar tipo de cambio por defecto si no existe
        var configuracionService = scope.ServiceProvider.GetRequiredService<IConfiguracionService>();
        configuracionService.CrearSiNoExiste(
            "TipoCambioDolar",
            SD.TipoCambioDolar.ToString("F2"),
            "Tipo de cambio dólar a córdoba (C$ por $1)"
        );

        // Inicializar datos básicos de inventario
        InicializarInventario.InicializarDatosBasicos(dbContext, logger);

        // Inicializar métodos de pago por defecto para landing page
        InicializarMetodosPago.CrearMetodosPagoDefectoSiNoExisten(dbContext, logger);

        // Inicializar servicios de landing page por defecto
        InicializarServiciosLandingPage.CrearServiciosLandingPageDefectoSiNoExisten(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // En desarrollo, solo usar HTTP si no hay HTTPS configurado
    // Esto evita la advertencia de redirección HTTPS
    app.UseDeveloperExceptionPage();
}

// Manejar códigos de estado (404, 403, 500, etc.)
app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");

// Configurar cache para archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Deshabilitar cache para desarrollo, pero permitir cache en producción con versionado
        if (app.Environment.IsDevelopment())
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    }
});

app.UseRouting();

// Habilitar CORS para la landing page (DEBE ir después de UseRouting y antes de UseAuthentication)
app.UseCors("LandingPagePolicy");

// Habilitar sesiones
app.UseSession();

// Habilitar Authentication y Authorization (debe ir después de UseRouting y antes de MapControllerRoute)
app.UseAuthentication();
app.UseAuthorization();


// Configurar rutas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Verificar y crear usuario demo si no existe (ejecutar solo una vez al inicio)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var existeDemo = dbContext.Usuarios.Any(u => u.NombreUsuario.ToLower() == "demo");
        if (!existeDemo)
        {
            logger.LogInformation("Usuario demo no existe. Creando...");
            InicializarUsuarioDemo.CrearDemoSiNoExiste(dbContext, logger);
        }
        else
        {
            // Verificar que la contraseña sea correcta
            var demo = dbContext.Usuarios.First(u => u.NombreUsuario.ToLower() == "demo");
            var hashCorrecto = PasswordHelper.HashPassword("demo");
            if (demo.Contrasena != hashCorrecto)
            {
                logger.LogInformation("Actualizando contraseña del usuario demo...");
                demo.Contrasena = hashCorrecto;
                dbContext.SaveChanges();
                logger.LogInformation("Contraseña del usuario demo actualizada.");
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al verificar/crear usuario demo");
    }
}

app.Run();

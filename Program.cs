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
using System.Text;

// Inicializar QuestPDF antes de cualquier uso
QuestPDF.Settings.License = LicenseType.Community;

// Configurar Npgsql para manejar DateTime correctamente con PostgreSQL
// Esto permite usar DateTime.Now sin especificar UTC explícitamente
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
builder.Services.AddControllersWithViews();

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
    options.AddPolicy("FacturasPagos", policy => policy.RequireClaim("Rol", "Normal", "Administrador"));
    options.AddPolicy("Pagos", policy => policy.RequireClaim("Rol", "Caja", "Normal", "Administrador"));
    options.AddPolicy("Inventario", policy => policy.RequireClaim("Rol", "Normal", "Administrador"));
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

// Servicios de Landing Page
builder.Services.AddScoped<IMetodoPagoService, MetodoPagoService>();
builder.Services.AddScoped<IServicioLandingPageService, ServicioLandingPageService>();

// Servicios de Egresos/Gastos
builder.Services.AddScoped<IEgresoService, EgresoService>();

// Servicios de Contacto (Landing Page)
builder.Services.AddScoped<IContactoService, ContactoService>();

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
        
        // Agregar columna Mensaje si no existe (workaround para migración pendiente)
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                ALTER TABLE ""ServiciosLandingPage"" 
                ADD COLUMN IF NOT EXISTS ""Mensaje"" character varying(500) NULL;
            ");
            logger.LogInformation("Columna 'Mensaje' verificada/agregada en ServiciosLandingPage");
        }
        catch (Exception ex)
        {
            // Si la columna ya existe, ignorar el error
            if (!ex.Message.Contains("already exists") && !ex.Message.Contains("duplicate"))
            {
                logger.LogWarning($"Advertencia al verificar columna Mensaje: {ex.Message}");
            }
        }
        
        // Crear tabla Egresos si no existe
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Egresos"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Codigo"" character varying(20) NOT NULL,
                    ""Descripcion"" character varying(500) NOT NULL,
                    ""Categoria"" character varying(100) NOT NULL,
                    ""Monto"" numeric(18,2) NOT NULL,
                    ""Fecha"" timestamp without time zone NOT NULL,
                    ""NumeroFactura"" character varying(100),
                    ""Proveedor"" character varying(200),
                    ""MetodoPago"" character varying(50) NOT NULL DEFAULT 'Efectivo',
                    ""Observaciones"" character varying(1000),
                    ""UsuarioId"" integer,
                    ""Activo"" boolean NOT NULL DEFAULT true,
                    ""FechaCreacion"" timestamp without time zone NOT NULL,
                    ""FechaActualizacion"" timestamp without time zone,
                    CONSTRAINT ""FK_Egresos_Usuarios_UsuarioId"" FOREIGN KEY (""UsuarioId"") REFERENCES ""Usuarios""(""Id"") ON DELETE SET NULL
                );
            ");
            
            // Crear índices si no existen
            dbContext.Database.ExecuteSqlRaw(@"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Egresos_Codigo"" ON ""Egresos"" (""Codigo"");");
            dbContext.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Egresos_Fecha"" ON ""Egresos"" (""Fecha"");");
            dbContext.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Egresos_Categoria"" ON ""Egresos"" (""Categoria"");");
            dbContext.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Egresos_UsuarioId"" ON ""Egresos"" (""UsuarioId"");");
            
            logger.LogInformation("Tabla 'Egresos' verificada/creada correctamente");
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("already exists") && !ex.Message.Contains("duplicate"))
            {
                logger.LogWarning($"Advertencia al crear tabla Egresos: {ex.Message}");
            }
        }
        
        // Crear tabla Contactos si no existe
        try
        {
            dbContext.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Contactos"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Nombre"" character varying(100) NOT NULL,
                    ""Correo"" character varying(150) NOT NULL,
                    ""Telefono"" character varying(20) NOT NULL,
                    ""Mensaje"" character varying(1000) NOT NULL,
                    ""FechaEnvio"" timestamp without time zone NOT NULL,
                    ""Estado"" character varying(20) NOT NULL DEFAULT 'Nuevo',
                    ""FechaLeido"" timestamp without time zone,
                    ""FechaRespondido"" timestamp without time zone
                );
            ");
            
            // Crear índices si no existen
            dbContext.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Contactos_FechaEnvio"" ON ""Contactos"" (""FechaEnvio"");");
            dbContext.Database.ExecuteSqlRaw(@"CREATE INDEX IF NOT EXISTS ""IX_Contactos_Estado"" ON ""Contactos"" (""Estado"");");
            
            logger.LogInformation("Tabla 'Contactos' verificada/creada correctamente");
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("already exists") && !ex.Message.Contains("duplicate"))
            {
                logger.LogWarning($"Advertencia al crear tabla Contactos: {ex.Message}");
            }
        }
        
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

app.Run();

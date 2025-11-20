using billing_system.Data;
using billing_system.Services;
using billing_system.Services.IServices;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Configurar URLs en minúsculas
builder.Services.AddRouting(options => options.LowercaseUrls = true);

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

var app = builder.Build();

// Inicializar almacenamiento en memoria
InMemoryStorage.Initialize();

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

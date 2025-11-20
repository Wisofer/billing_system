namespace billing_system.Middlewares;

public class JwtRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public JwtRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Si la respuesta es 401 y no es la página de login, redirigir al login
        if (context.Response.StatusCode == 401 && 
            !context.Request.Path.StartsWithSegments("/login"))
        {
            context.Response.Redirect("/login");
        }
    }
}


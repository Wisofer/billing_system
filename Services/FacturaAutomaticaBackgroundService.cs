using billing_system.Services.IServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace billing_system.Services;

/// <summary>
/// Servicio en segundo plano que genera facturas automáticamente el día 1 de cada mes a las 2:00 AM
/// </summary>
public class FacturaAutomaticaBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FacturaAutomaticaBackgroundService> _logger;
    private DateTime? _ultimaEjecucion = null;

    public FacturaAutomaticaBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FacturaAutomaticaBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de generación automática de facturas iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ahora = DateTime.Now;
                
                // Verificar si es el día 1 del mes y la hora es 2:00 AM (o entre 2:00 y 2:59)
                // También verificamos que no se haya ejecutado ya hoy
                bool esDiaUno = ahora.Day == 1;
                bool esHoraCorrecta = ahora.Hour == 2;
                bool yaEjecutadoHoy = _ultimaEjecucion.HasValue && 
                                     _ultimaEjecucion.Value.Date == ahora.Date;

                if (esDiaUno && esHoraCorrecta && !yaEjecutadoHoy)
                {
                    _logger.LogInformation(
                        "Condiciones cumplidas: Día 1 del mes a las 2:00 AM. Iniciando generación automática de facturas...");

                    // Crear un scope para obtener los servicios necesarios
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var facturaService = scope.ServiceProvider.GetRequiredService<IFacturaService>();
                        
                        try
                        {
                            facturaService.GenerarFacturasAutomaticas();
                            _ultimaEjecucion = ahora;
                            
                            _logger.LogInformation(
                                "Facturas automáticas generadas exitosamente el {Fecha}",
                                ahora.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Error al generar facturas automáticas el {Fecha}",
                                ahora.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                    }
                }

                // Esperar 1 hora antes de verificar nuevamente
                // Esto asegura que solo se ejecute una vez por día cuando sea el momento correcto
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // El servicio está siendo detenido
                _logger.LogInformation("Servicio de generación automática de facturas está siendo detenido.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en el servicio de generación automática de facturas.");
                // Esperar 1 hora antes de intentar nuevamente
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Servicio de generación automática de facturas detenido.");
    }
}


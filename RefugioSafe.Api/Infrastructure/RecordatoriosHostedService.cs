using Microsoft.Extensions.Options;
using RefugioSafe.Api.Application;

namespace RefugioSafe.Api.Infrastructure;

/// <summary>Contenedor "Background Service" del C4 (nivel 2): revisa periódicamente vacunas y controles próximos.</summary>
public class RecordatoriosHostedService(
    IServiceScopeFactory scopes,
    IOptions<RecordatoriosOptions> opciones,
    ILogger<RecordatoriosHostedService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, opciones.Value.IntervaloMinutos)));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var servicio = scope.ServiceProvider.GetRequiredService<RecordatorioService>();
                    var enviados = await servicio.ProcesarAsync();
                    if (enviados > 0) log.LogInformation("Recordatorios procesados: {Cantidad}", enviados);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error procesando recordatorios.");
                }
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) { /* apagado normal */ }
    }
}

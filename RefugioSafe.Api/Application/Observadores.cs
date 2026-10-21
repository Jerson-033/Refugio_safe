using RefugioSafe.Api.Domain;

namespace RefugioSafe.Api.Application;

// ===================== PATRÓN OBSERVER: receptores =====================

/// <summary>RN-05: tras una decisión recalcula e informa la capacidad (capacidadActual es calculada, no almacenada).</summary>
public class CapacidadObserver(CapacidadService capacidad, ILogger<CapacidadObserver> log) : IAdopcionObserver
{
    public async Task AdopcionFinalizadaAsync(Adopcion a, CancellationToken ct = default)
    {
        var c = await capacidad.ConsultarAsync();
        log.LogInformation("Adopción #{Id} {Estado}. Capacidad: {Ocupados}/{Total} ocupados, {Disp} disponibles.",
            a.Id, a.Estado, c.Ocupados, c.Total, c.Disponibles);
    }
}

/// <summary>HU-06: solo las adopciones aprobadas tienen seguimiento; se programa el primer control.</summary>
public class SeguimientoObserver(ISeguimientoRepository seguimientos, IUnitOfWork uow, TimeProvider reloj) : IAdopcionObserver
{
    private const int DiasHastaPrimerControl = 15;

    public async Task AdopcionFinalizadaAsync(Adopcion a, CancellationToken ct = default)
    {
        if (a.Estado != EstadoAdopcion.Aprobada) return;

        await seguimientos.AgregarAsync(new Seguimiento
        {
            AdopcionId = a.Id,
            FechaControl = reloj.GetLocalNow().Date.AddDays(DiasHastaPrimerControl)
        });
        await uow.GuardarCambiosAsync();
    }
}

/// <summary>HU-07: avisa al adoptante el resultado; delega el envío en INotificador (Strategy).</summary>
public class NotificacionObserver(NotificacionService notificaciones) : IAdopcionObserver
{
    public Task AdopcionFinalizadaAsync(Adopcion a, CancellationToken ct = default)
    {
        var aprobada = a.Estado == EstadoAdopcion.Aprobada;
        var asunto = aprobada ? "¡Tu adopción fue aprobada!" : "Resultado de tu solicitud de adopción";
        var mensaje = aprobada
            ? $"Hola {a.Adoptante.NombreCompleto}, tu solicitud para adoptar a {a.Animal.Nombre} fue aprobada. ¡Gracias por darle un hogar!"
            : $"Hola {a.Adoptante.NombreCompleto}, tu solicitud para adoptar a {a.Animal.Nombre} no fue aprobada. {a.Observaciones}";

        return notificaciones.EnviarAsync(TipoNotificacion.ResultadoAdopcion, a.Animal, a.Adoptante, asunto, mensaje);
    }
}

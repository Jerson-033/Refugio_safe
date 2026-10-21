namespace RefugioSafe.Api.Domain;

// ===================== PATRÓN STATE =====================
public interface IEstadoAdopcion
{
    void Evaluar(Adopcion adopcion, bool apto, string? observaciones);
    void Aprobar(Adopcion adopcion, string? observaciones);
    void Rechazar(Adopcion adopcion, string? observaciones);
}

public abstract class EstadoAdopcionBase(EstadoAdopcion codigo) : IEstadoAdopcion
{
    private ReglaNegocioException NoPermitido() =>
        new($"Operación no permitida: la solicitud está en estado '{codigo}'.");

    public virtual void Evaluar(Adopcion a, bool apto, string? obs) => throw NoPermitido();
    public virtual void Aprobar(Adopcion a, string? obs) => throw NoPermitido();
    public virtual void Rechazar(Adopcion a, string? obs) => throw NoPermitido();

    protected static void Anotar(Adopcion a, string? obs)
    {
        if (!string.IsNullOrWhiteSpace(obs)) a.Observaciones = obs;
    }

    // RN-04: un rechazo libera automáticamente al animal a 'Disponible'
    protected static void FinalizarRechazo(Adopcion a, string? obs)
    {
        a.Estado = EstadoAdopcion.Rechazada;
        a.Animal.Estado = EstadoAnimal.Disponible;
        Anotar(a, obs);
    }
}

public class EstadoIniciada() : EstadoAdopcionBase(EstadoAdopcion.Iniciada)
{
    public override void Evaluar(Adopcion a, bool apto, string? obs)
    {
        a.Estado = apto ? EstadoAdopcion.EvaluadaApta : EstadoAdopcion.EvaluadaNoApta;
        Anotar(a, obs);
    }

    // RN-03: no se aprueba sin evaluación previa 'Apto'
    public override void Aprobar(Adopcion a, string? obs) =>
        throw new ReglaNegocioException("No se puede aprobar la adopción sin una evaluación registrada como 'Apto' (RN-03). Complete la evaluación.");

    public override void Rechazar(Adopcion a, string? obs) => FinalizarRechazo(a, obs);
}

public class EstadoEvaluadaApta() : EstadoAdopcionBase(EstadoAdopcion.EvaluadaApta)
{
    public override void Aprobar(Adopcion a, string? obs)
    {
        a.Estado = EstadoAdopcion.Aprobada;
        a.Animal.Estado = EstadoAnimal.Adoptado;
        a.FechaAdopcion = DateTime.Now;
        Anotar(a, obs);
    }

    public override void Rechazar(Adopcion a, string? obs) => FinalizarRechazo(a, obs);
}

public class EstadoEvaluadaNoApta() : EstadoAdopcionBase(EstadoAdopcion.EvaluadaNoApta)
{
    public override void Aprobar(Adopcion a, string? obs) =>
        throw new ReglaNegocioException("La evaluación del adoptante es 'No Apto'; no se puede aprobar la adopción (RN-03).");

    public override void Rechazar(Adopcion a, string? obs) => FinalizarRechazo(a, obs);
}

public class EstadoAprobada() : EstadoAdopcionBase(EstadoAdopcion.Aprobada);   // estado final
public class EstadoRechazada() : EstadoAdopcionBase(EstadoAdopcion.Rechazada); // estado final

public static class FabricaEstados
{
    private static readonly Dictionary<EstadoAdopcion, IEstadoAdopcion> Estados = new()
    {
        [EstadoAdopcion.Iniciada] = new EstadoIniciada(),
        [EstadoAdopcion.EvaluadaApta] = new EstadoEvaluadaApta(),
        [EstadoAdopcion.EvaluadaNoApta] = new EstadoEvaluadaNoApta(),
        [EstadoAdopcion.Aprobada] = new EstadoAprobada(),
        [EstadoAdopcion.Rechazada] = new EstadoRechazada()
    };

    public static IEstadoAdopcion Crear(EstadoAdopcion estado) => Estados[estado];
}

namespace RefugioSafe.Api.Domain;

public class ReglaNegocioException(string mensaje) : Exception(mensaje);

// Enumeraciones (Tabla 1 del Entregable 2: enumeraciones con sus valores)
public enum EstadoAnimal { Disponible, EnProceso, Adoptado }
public enum EstadoAdopcion { Iniciada, EvaluadaApta, EvaluadaNoApta, Aprobada, Rechazada }
public enum EstadoSeguimiento { Pendiente, Realizado }
public enum TipoNotificacion { RecordatorioVacuna, ControlVeterinario, ResultadoAdopcion }
public enum EstadoNotificacion { Pendiente, Enviada }
public enum RolUsuario { Administrador, PersonalRefugio }

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Personal = "PersonalRefugio";
}

public class Refugio
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Ubicacion { get; set; } = string.Empty;
    public int CapacidadTotal { get; set; }   // capacidadActual se calcula (no se guarda)
}

public class Animal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string EstadoSalud { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
    public string? FotoUrl { get; set; }
    public EstadoAnimal Estado { get; set; } = EstadoAnimal.Disponible;
    public List<Vacuna> Vacunas { get; set; } = new();
}

public class Vacuna
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaAplicada { get; set; }
    public bool RecordatorioEnviado { get; set; }
}

public class Adoptante
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = string.Empty;
    public string NumeroDocumento { get; set; } = string.Empty;
    public string Contacto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
}

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
}

public class Seguimiento
{
    public int Id { get; set; }
    public int AdopcionId { get; set; }
    public Adopcion Adopcion { get; set; } = null!;
    public DateTime FechaControl { get; set; }
    public EstadoSeguimiento Estado { get; set; } = EstadoSeguimiento.Pendiente;
    public string? Resultado { get; set; }
    public bool RecordatorioEnviado { get; set; }
}

public class Notificacion
{
    public int Id { get; set; }
    public TipoNotificacion Tipo { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public int AdoptanteId { get; set; }
    public Adoptante Adoptante { get; set; } = null!;
    public string EnviadaPor { get; set; } = "Sistema";
    public DateTime Fecha { get; set; }
    public EstadoNotificacion Estado { get; set; } = EstadoNotificacion.Pendiente;
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Contexto del patrón STATE. El estado se persiste como enum (Estado) y el comportamiento
/// se delega al objeto de estado correspondiente (IEstadoAdopcion).
/// </summary>
public class Adopcion
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public Animal Animal { get; set; } = null!;
    public int AdoptanteId { get; set; }
    public Adoptante Adoptante { get; set; } = null!;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaAdopcion { get; set; }
    public EstadoAdopcion Estado { get; set; } = EstadoAdopcion.Iniciada;
    public string? Observaciones { get; set; }

    private IEstadoAdopcion EstadoActual => FabricaEstados.Crear(Estado);

    // HU-04: solo un animal 'Disponible' puede iniciar adopción (un animal = una adopción activa)
    public static Adopcion Iniciar(Animal animal, Adoptante adoptante)
    {
        if (animal.Estado != EstadoAnimal.Disponible)
            throw new ReglaNegocioException($"El animal '{animal.Nombre}' no está disponible para adopción.");

        animal.Estado = EstadoAnimal.EnProceso;
        return new Adopcion
        {
            Animal = animal,
            Adoptante = adoptante,
            FechaSolicitud = DateTime.Now
        };
    }

    public void Evaluar(bool apto, string? observaciones) => EstadoActual.Evaluar(this, apto, observaciones);
    public void Aprobar(string? observaciones) => EstadoActual.Aprobar(this, observaciones);
    public void Rechazar(string? observaciones) => EstadoActual.Rechazar(this, observaciones);
}

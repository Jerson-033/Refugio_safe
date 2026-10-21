using RefugioSafe.Api.Domain;

namespace RefugioSafe.Api.Application;

// ===== Abstracciones (DIP + ISP): contratos pequeños y específicos por agregado =====
public interface IUnitOfWork { Task GuardarCambiosAsync(); }

public interface IAnimalRepository
{
    Task<List<Animal>> ListarAsync(EstadoAnimal? estado);
    Task<Animal?> ObtenerAsync(int id);
    Task<bool> ExisteDuplicadoAsync(string nombre, string especie, string raza, DateTime fechaIngreso);
    Task AgregarAsync(Animal animal);
    Task<int> ContarAlojadosAsync();
    Task<Dictionary<string, int>> ContarAlojadosPorEspecieAsync();
}

public interface IAdoptanteRepository
{
    Task<List<Adoptante>> ListarAsync();
    Task<Adoptante?> ObtenerAsync(int id);
    Task<bool> ExisteDocumentoAsync(string numeroDocumento);
    Task AgregarAsync(Adoptante adoptante);
}

public interface IAdopcionRepository
{
    Task<List<Adopcion>> ListarAsync(EstadoAdopcion? estado);
    Task<Adopcion?> ObtenerAsync(int id);
    Task<Adopcion?> ObtenerAprobadaPorAnimalAsync(int animalId);
    Task AgregarAsync(Adopcion adopcion);
}

public interface ISeguimientoRepository
{
    Task<List<Seguimiento>> ListarAsync();
    Task<Seguimiento?> ObtenerAsync(int id);
    Task AgregarAsync(Seguimiento seguimiento);
}

public interface INotificacionRepository
{
    Task<List<Notificacion>> ListarAsync();
    Task AgregarAsync(Notificacion notificacion);
}

public interface IRecordatorioRepository
{
    Task<List<Vacuna>> VacunasPorNotificarAsync(DateTime limite);
    Task<List<Seguimiento>> SeguimientosPorNotificarAsync(DateTime limite);
}

public interface IUsuarioRepository { Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario); }
public interface IRefugioRepository { Task<Refugio> ObtenerAsync(); }

// ===== STRATEGY: cómo se envía un aviso =====
public interface INotificador
{
    Task<bool> EnviarAsync(string destinatario, string asunto, string mensaje);
}

// ===== OBSERVER: quién reacciona cuando una adopción se aprueba o se rechaza =====
public interface IAdopcionObserver
{
    Task AdopcionFinalizadaAsync(Adopcion adopcion, CancellationToken ct = default);
}

// ===== Opciones de configuración =====
public class JwtOptions { public string Clave { get; set; } = ""; public string Emisor { get; set; } = "RefugioSafe"; }
public class RecordatoriosOptions { public int DiasAnticipacion { get; set; } = 3; public int IntervaloMinutos { get; set; } = 1; }
public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = "";
    public string Clave { get; set; } = "";
    public string Remitente { get; set; } = "";
}

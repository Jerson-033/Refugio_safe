namespace RefugioSafe.Contracts;

// Autenticación (RF-11)
public record LoginRequest(string Usuario, string Password);
public record LoginResponse(string Token, string Usuario, string Rol);

// Animales (RF-01, RF-02, RF-10)
public record AnimalDto(int Id, string Nombre, string Especie, string Raza, int Edad, string EstadoSalud, string Estado, DateTime FechaIngreso, string? FotoUrl);
public record AnimalDetalleDto(AnimalDto Animal, List<VacunaDto> Vacunas);
public record CrearAnimalRequest(string Nombre, string Especie, string Raza, int Edad, string EstadoSalud, DateTime FechaIngreso, string? FotoUrl);
public record VacunaDto(int Id, string Nombre, DateTime FechaProgramada, DateTime? FechaAplicada);
public record ProgramarVacunaRequest(string Nombre, DateTime FechaProgramada);

// Adoptantes (RF-03)
public record AdoptanteDto(int Id, string NombreCompleto, string TipoDocumento, string NumeroDocumento, string Contacto, string Correo, string Direccion);
public record CrearAdoptanteRequest(string NombreCompleto, string TipoDocumento, string NumeroDocumento, string Contacto, string Correo, string Direccion);

// Adopciones (RF-04, RF-05, RF-09)
public record AdopcionDto(int Id, int AnimalId, string Animal, int AdoptanteId, string Adoptante, string Estado, DateTime FechaSolicitud, DateTime? FechaAdopcion, string? Observaciones);
public record IniciarAdopcionRequest(int AnimalId, int AdoptanteId);
public record EvaluarRequest(bool Apto, string? Observaciones);
public record DecisionRequest(string? Observaciones);

// Seguimiento (RF-06)
public record SeguimientoDto(int Id, int AdopcionId, string Animal, string Adoptante, DateTime FechaControl, string Estado, string? Resultado);
public record ProgramarSeguimientoRequest(int AdopcionId, DateTime FechaControl);
public record RegistrarResultadoRequest(string Resultado);

// Notificaciones (RF-07)
public record NotificacionDto(int Id, string Animal, string Adoptante, string Tipo, string EnviadaPor, DateTime Fecha, string Estado);
public record ProcesarResultadoDto(int Enviados);

// Capacidad (RF-08)
public record CapacidadDto(string Refugio, int Total, int Ocupados, int Disponibles, double PorcentajeOcupacion, Dictionary<string, int> PorEspecie);
public record ActualizarCapacidadRequest(int CapacidadTotal);

public record ErrorDto(string Mensaje);

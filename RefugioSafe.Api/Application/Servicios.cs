using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RefugioSafe.Api.Domain;
using RefugioSafe.Contracts;

namespace RefugioSafe.Api.Application;

// ===================== RF-11: autenticación =====================
public class AuthService(IUsuarioRepository usuarios, IOptions<JwtOptions> jwt)
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public async Task<LoginResponse?> LoginAsync(string nombreUsuario, string password)
    {
        var u = await usuarios.ObtenerPorNombreAsync(nombreUsuario);
        if (u is null) return null;
        if (_hasher.VerifyHashedPassword(u, u.PasswordHash, password) == PasswordVerificationResult.Failed) return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Clave));
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, u.NombreUsuario),
            new Claim(ClaimTypes.Role, u.Rol.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: jwt.Value.Emisor,
            audience: jwt.Value.Emisor,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), u.NombreUsuario, u.Rol.ToString());
    }
}

// ===================== RF-08 / RN-05: capacidad =====================
public class CapacidadService(IRefugioRepository refugio, IAnimalRepository animales, IUnitOfWork uow)
{
    public async Task<CapacidadDto> ConsultarAsync()
    {
        var r = await refugio.ObtenerAsync();
        var ocupados = await animales.ContarAlojadosAsync();
        var porEspecie = await animales.ContarAlojadosPorEspecieAsync();
        var pct = r.CapacidadTotal == 0 ? 0 : Math.Round(100.0 * ocupados / r.CapacidadTotal, 1);
        return new CapacidadDto(r.Nombre, r.CapacidadTotal, ocupados, r.CapacidadTotal - ocupados, pct, porEspecie);
    }

    public async Task<CapacidadDto> ActualizarAsync(int nuevaCapacidad)
    {
        var ocupados = await animales.ContarAlojadosAsync();
        if (nuevaCapacidad < ocupados)
            throw new ReglaNegocioException($"La capacidad total no puede ser menor a los animales alojados ({ocupados}) (RN-05).");

        var r = await refugio.ObtenerAsync();
        r.CapacidadTotal = nuevaCapacidad;
        await uow.GuardarCambiosAsync();
        return await ConsultarAsync();
    }
}

// ===================== RF-01, RF-02, RF-10: animales =====================
public class AnimalService(IAnimalRepository animales, IRefugioRepository refugio, IUnitOfWork uow)
{
    public Task<List<Animal>> ListarAsync(EstadoAnimal? estado) => animales.ListarAsync(estado);

    public async Task<Animal> ObtenerAsync(int id) =>
        await animales.ObtenerAsync(id) ?? throw new KeyNotFoundException("Animal no encontrado.");

    public async Task<Animal> RegistrarAsync(CrearAnimalRequest r)
    {
        // RNF-03: campos obligatorios
        if (string.IsNullOrWhiteSpace(r.Nombre) || string.IsNullOrWhiteSpace(r.Especie) ||
            string.IsNullOrWhiteSpace(r.Raza) || string.IsNullOrWhiteSpace(r.EstadoSalud))
            throw new ReglaNegocioException("Nombre, especie, raza y estado de salud son obligatorios.");
        if (r.Edad < 0) throw new ReglaNegocioException("La edad no puede ser negativa.");

        var nombre = r.Nombre.Trim();
        var especie = r.Especie.Trim();
        var raza = r.Raza.Trim();
        var fecha = r.FechaIngreso.Date;

        // RN-01 (refinada): duplicado = nombre + especie + raza + fecha de ingreso
        if (await animales.ExisteDuplicadoAsync(nombre, especie, raza, fecha))
            throw new ReglaNegocioException("Ya existe un animal con el mismo nombre, especie, raza y fecha de ingreso (RN-01).");

        // Mantiene vigente RN-05: no se alojan más animales que la capacidad total
        var capacidad = (await refugio.ObtenerAsync()).CapacidadTotal;
        if (await animales.ContarAlojadosAsync() >= capacidad)
            throw new ReglaNegocioException("El refugio no tiene capacidad disponible para alojar más animales.");

        var animal = new Animal
        {
            Nombre = nombre, Especie = especie, Raza = raza, Edad = r.Edad,
            EstadoSalud = r.EstadoSalud.Trim(), FechaIngreso = fecha, FotoUrl = r.FotoUrl,
            Estado = EstadoAnimal.Disponible
        };
        await animales.AgregarAsync(animal);
        await uow.GuardarCambiosAsync();
        return animal;
    }

    public async Task<Vacuna> ProgramarVacunaAsync(int animalId, ProgramarVacunaRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nombre)) throw new ReglaNegocioException("El nombre de la vacuna es obligatorio.");
        var animal = await ObtenerAsync(animalId);
        var vacuna = new Vacuna { Nombre = r.Nombre.Trim(), FechaProgramada = r.FechaProgramada.Date };
        animal.Vacunas.Add(vacuna);
        await uow.GuardarCambiosAsync();
        return vacuna;
    }
}

// ===================== RF-03 / RN-02: adoptantes =====================
public class AdoptanteService(IAdoptanteRepository adoptantes, IUnitOfWork uow)
{
    public Task<List<Adoptante>> ListarAsync() => adoptantes.ListarAsync();

    public async Task<Adoptante> RegistrarAsync(CrearAdoptanteRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.NombreCompleto) || string.IsNullOrWhiteSpace(r.NumeroDocumento) ||
            string.IsNullOrWhiteSpace(r.Correo))
            throw new ReglaNegocioException("Nombre completo, número de documento y correo son obligatorios.");

        var documento = r.NumeroDocumento.Trim();
        if (await adoptantes.ExisteDocumentoAsync(documento))
            throw new ReglaNegocioException("Ya existe un adoptante registrado con ese documento (RN-02).");

        var a = new Adoptante
        {
            NombreCompleto = r.NombreCompleto.Trim(), TipoDocumento = r.TipoDocumento, NumeroDocumento = documento,
            Contacto = r.Contacto?.Trim() ?? "", Correo = r.Correo.Trim(), Direccion = r.Direccion?.Trim() ?? ""
        };
        await adoptantes.AgregarAsync(a);
        await uow.GuardarCambiosAsync();
        return a;
    }
}

// ===================== RF-04, RF-05, RF-09: adopciones (SUJETO del Observer) =====================
public class AdopcionService(
    IAdopcionRepository adopciones,
    IAnimalRepository animales,
    IAdoptanteRepository adoptantes,
    IUnitOfWork uow,
    IEnumerable<IAdopcionObserver> observadores,
    ILogger<AdopcionService> log)
{
    public Task<List<Adopcion>> ListarAsync(EstadoAdopcion? estado) => adopciones.ListarAsync(estado);

    public async Task<Adopcion> IniciarAsync(int animalId, int adoptanteId)
    {
        var animal = await animales.ObtenerAsync(animalId) ?? throw new KeyNotFoundException("Animal no encontrado.");
        var adoptante = await adoptantes.ObtenerAsync(adoptanteId) ?? throw new KeyNotFoundException("Adoptante no encontrado.");

        var adopcion = Adopcion.Iniciar(animal, adoptante);      // valida 'Disponible' y pasa el animal a 'EnProceso'
        await adopciones.AgregarAsync(adopcion);
        await uow.GuardarCambiosAsync();
        return adopcion;
    }

    public async Task<Adopcion> EvaluarAsync(int id, bool apto, string? observaciones)
    {
        var a = await ObtenerAsync(id);
        a.Evaluar(apto, observaciones);                          // State
        await uow.GuardarCambiosAsync();
        return a;
    }

    public async Task<Adopcion> AprobarAsync(int id, string? observaciones)
    {
        var a = await ObtenerAsync(id);
        a.Aprobar(observaciones);                                // State (RN-03)
        await uow.GuardarCambiosAsync();
        await NotificarObservadoresAsync(a);                     // Observer
        return a;
    }

    public async Task<Adopcion> RechazarAsync(int id, string? observaciones)
    {
        var a = await ObtenerAsync(id);
        a.Rechazar(observaciones);                               // State (RN-04)
        await uow.GuardarCambiosAsync();
        await NotificarObservadoresAsync(a);                     // Observer
        return a;
    }

    private async Task<Adopcion> ObtenerAsync(int id) =>
        await adopciones.ObtenerAsync(id) ?? throw new KeyNotFoundException("Solicitud de adopción no encontrada.");

    private async Task NotificarObservadoresAsync(Adopcion adopcion)
    {
        foreach (var observador in observadores)
        {
            try { await observador.AdopcionFinalizadaAsync(adopcion); }
            catch (Exception ex)
            {
                // Un observador que falla no debe deshacer la decisión ya registrada
                log.LogError(ex, "El observador {Observador} falló.", observador.GetType().Name);
            }
        }
    }
}

// ===================== RF-06: seguimiento post-adopción =====================
public class SeguimientoService(ISeguimientoRepository seguimientos, IAdopcionRepository adopciones, IUnitOfWork uow)
{
    public Task<List<Seguimiento>> ListarAsync() => seguimientos.ListarAsync();

    public async Task<Seguimiento> ProgramarAsync(int adopcionId, DateTime fechaControl)
    {
        var adopcion = await adopciones.ObtenerAsync(adopcionId) ?? throw new KeyNotFoundException("Adopción no encontrada.");
        if (adopcion.Estado != EstadoAdopcion.Aprobada)
            throw new ReglaNegocioException("Solo las adopciones aprobadas tienen seguimiento.");

        var s = new Seguimiento { AdopcionId = adopcionId, FechaControl = fechaControl.Date };
        await seguimientos.AgregarAsync(s);
        await uow.GuardarCambiosAsync();
        return s;
    }

    public async Task<Seguimiento> RegistrarResultadoAsync(int id, string resultado)
    {
        if (string.IsNullOrWhiteSpace(resultado)) throw new ReglaNegocioException("El resultado del control es obligatorio.");
        var s = await seguimientos.ObtenerAsync(id) ?? throw new KeyNotFoundException("Control no encontrado.");
        s.Estado = EstadoSeguimiento.Realizado;
        s.Resultado = resultado.Trim();
        await uow.GuardarCambiosAsync();
        return s;
    }
}

// ===================== RF-07: notificaciones (usa STRATEGY) =====================
public class NotificacionService(INotificacionRepository repo, INotificador notificador, IUnitOfWork uow, ILogger<NotificacionService> log)
{
    public Task<List<Notificacion>> ListarAsync() => repo.ListarAsync();

    public async Task EnviarAsync(TipoNotificacion tipo, Animal animal, Adoptante adoptante,
                                  string asunto, string mensaje, string enviadaPor = "Sistema")
    {
        var n = new Notificacion
        {
            Tipo = tipo, AnimalId = animal.Id, AdoptanteId = adoptante.Id,
            EnviadaPor = enviadaPor, Fecha = DateTime.Now, Mensaje = mensaje,
            Estado = EstadoNotificacion.Pendiente
        };
        await repo.AgregarAsync(n);

        try
        {
            // La estrategia concreta (correo o simulada) la decide la configuración
            if (await notificador.EnviarAsync(adoptante.Correo, asunto, mensaje))
                n.Estado = EstadoNotificacion.Enviada;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "No se pudo enviar la notificación a {Correo}; queda Pendiente.", adoptante.Correo);
        }

        await uow.GuardarCambiosAsync();
    }
}

// ===================== HU-07: verificación periódica de recordatorios =====================
public class RecordatorioService(
    IRecordatorioRepository repo,
    IAdopcionRepository adopciones,
    NotificacionService notificaciones,
    IUnitOfWork uow,
    IOptions<RecordatoriosOptions> opciones,
    TimeProvider reloj)
{
    public async Task<int> ProcesarAsync()
    {
        var hoy = reloj.GetLocalNow().Date;
        var limite = hoy.AddDays(opciones.Value.DiasAnticipacion);
        var enviados = 0;

        foreach (var v in await repo.VacunasPorNotificarAsync(limite))
        {
            var adopcion = await adopciones.ObtenerAprobadaPorAnimalAsync(v.AnimalId);
            if (adopcion is null) continue;   // sin adoptante no hay a quién avisar

            await notificaciones.EnviarAsync(TipoNotificacion.RecordatorioVacuna, adopcion.Animal, adopcion.Adoptante,
                "Recordatorio de vacuna",
                $"Hola {adopcion.Adoptante.NombreCompleto}, {adopcion.Animal.Nombre} tiene programada la vacuna '{v.Nombre}' el {v.FechaProgramada:dd/MM/yyyy}.");
            v.RecordatorioEnviado = true;
            enviados++;
        }

        foreach (var s in await repo.SeguimientosPorNotificarAsync(limite))
        {
            await notificaciones.EnviarAsync(TipoNotificacion.ControlVeterinario, s.Adopcion.Animal, s.Adopcion.Adoptante,
                "Recordatorio de control veterinario",
                $"Hola {s.Adopcion.Adoptante.NombreCompleto}, {s.Adopcion.Animal.Nombre} tiene un control de seguimiento el {s.FechaControl:dd/MM/yyyy}.");
            s.RecordatorioEnviado = true;
            enviados++;
        }

        await uow.GuardarCambiosAsync();
        return enviados;
    }
}

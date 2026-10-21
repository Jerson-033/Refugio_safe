using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefugioSafe.Api.Application;
using RefugioSafe.Api.Domain;
using RefugioSafe.Contracts;

namespace RefugioSafe.Api.Controllers;

// RNF-02: solo usuarios autenticados; funciones críticas solo para administradores.

[ApiController, Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest r)
    {
        var res = await auth.LoginAsync(r.Usuario, r.Password);
        return res is null ? Unauthorized(new ErrorDto("Usuario o contraseña incorrectos.")) : Ok(res);
    }
}

[ApiController, Route("api/capacidad"), Authorize]
public class CapacidadController(CapacidadService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CapacidadDto>> Consultar() => await svc.ConsultarAsync();

    [HttpPut, Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<CapacidadDto>> Actualizar(ActualizarCapacidadRequest r) =>
        await svc.ActualizarAsync(r.CapacidadTotal);
}

[ApiController, Route("api/animales"), Authorize]
public class AnimalesController(AnimalService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AnimalDto>>> Listar([FromQuery] string? estado)
    {
        EstadoAnimal? filtro = Enum.TryParse<EstadoAnimal>(estado, true, out var e) ? e : null;
        return (await svc.ListarAsync(filtro)).Select(a => a.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AnimalDetalleDto>> Obtener(int id)
    {
        var a = await svc.ObtenerAsync(id);
        return new AnimalDetalleDto(a.ToDto(), a.Vacunas.OrderBy(v => v.FechaProgramada).Select(v => v.ToDto()).ToList());
    }

    [HttpPost, Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<AnimalDto>> Registrar(CrearAnimalRequest r) => (await svc.RegistrarAsync(r)).ToDto();

    [HttpPost("{id:int}/vacunas"), Authorize(Roles = Roles.Administrador)]
    public async Task<ActionResult<VacunaDto>> ProgramarVacuna(int id, ProgramarVacunaRequest r) =>
        (await svc.ProgramarVacunaAsync(id, r)).ToDto();
}

[ApiController, Route("api/adoptantes"), Authorize(Roles = Roles.Administrador)]
public class AdoptantesController(AdoptanteService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdoptanteDto>>> Listar() => (await svc.ListarAsync()).Select(a => a.ToDto()).ToList();

    [HttpPost]
    public async Task<ActionResult<AdoptanteDto>> Registrar(CrearAdoptanteRequest r) => (await svc.RegistrarAsync(r)).ToDto();
}

[ApiController, Route("api/adopciones"), Authorize(Roles = Roles.Administrador)]
public class AdopcionesController(AdopcionService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdopcionDto>>> Listar([FromQuery] string? estado)
    {
        EstadoAdopcion? filtro = Enum.TryParse<EstadoAdopcion>(estado, true, out var e) ? e : null;
        return (await svc.ListarAsync(filtro)).Select(a => a.ToDto()).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<AdopcionDto>> Iniciar(IniciarAdopcionRequest r) =>
        (await svc.IniciarAsync(r.AnimalId, r.AdoptanteId)).ToDto();

    [HttpPost("{id:int}/evaluar")]
    public async Task<ActionResult<AdopcionDto>> Evaluar(int id, EvaluarRequest r) =>
        (await svc.EvaluarAsync(id, r.Apto, r.Observaciones)).ToDto();

    [HttpPost("{id:int}/aprobar")]
    public async Task<ActionResult<AdopcionDto>> Aprobar(int id, DecisionRequest r) =>
        (await svc.AprobarAsync(id, r.Observaciones)).ToDto();

    [HttpPost("{id:int}/rechazar")]
    public async Task<ActionResult<AdopcionDto>> Rechazar(int id, DecisionRequest r) =>
        (await svc.RechazarAsync(id, r.Observaciones)).ToDto();
}

[ApiController, Route("api/seguimientos"), Authorize(Roles = Roles.Administrador)]
public class SeguimientosController(SeguimientoService svc) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SeguimientoDto>>> Listar() => (await svc.ListarAsync()).Select(s => s.ToDto()).ToList();

    [HttpPost]
    public async Task<IActionResult> Programar(ProgramarSeguimientoRequest r)
    {
        var s = await svc.ProgramarAsync(r.AdopcionId, r.FechaControl);
        return Ok(new { s.Id });
    }

    [HttpPost("{id:int}/resultado")]
    public async Task<IActionResult> RegistrarResultado(int id, RegistrarResultadoRequest r)
    {
        await svc.RegistrarResultadoAsync(id, r.Resultado);
        return NoContent();
    }
}

[ApiController, Route("api/notificaciones"), Authorize(Roles = Roles.Administrador)]
public class NotificacionesController(NotificacionService svc, RecordatorioService recordatorios) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificacionDto>>> Listar() => (await svc.ListarAsync()).Select(n => n.ToDto()).ToList();

    // Permite ejecutar manualmente la verificación que normalmente hace el Background Service
    [HttpPost("procesar")]
    public async Task<ActionResult<ProcesarResultadoDto>> Procesar() => new ProcesarResultadoDto(await recordatorios.ProcesarAsync());
}

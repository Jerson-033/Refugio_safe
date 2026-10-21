using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RefugioSafe.Contracts;
using RefugioSafe.Web.Models;
using RefugioSafe.Web.Services;

namespace RefugioSafe.Web.Controllers;

public abstract class ControladorBase : Controller
{
    /// <summary>Ejecuta una llamada a la API y deja el resultado en TempData (Ok / Error).</summary>
    protected async Task<bool> Intentar(Func<Task> accion, string? exito = null)
    {
        try
        {
            await accion();
            if (exito != null) TempData["Ok"] = exito;
            return true;
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        catch (HttpRequestException) { TempData["Error"] = "No se pudo conectar con la API. ¿Está en ejecución RefugioSafe.Api?"; }
        return false;
    }
}

// ============ RF-11: inicio de sesión según rol ============
public class CuentaController(ApiClient api) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Login(string usuario, string password, string? returnUrl)
    {
        LoginResponse? r;
        try { r = await api.LoginAsync(usuario, password); }
        catch (HttpRequestException)
        {
            ViewBag.Error = "No se pudo conectar con la API. ¿Está en ejecución RefugioSafe.Api?";
            return View();
        }

        if (r is null)
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, r.Usuario),
            new(ClaimTypes.Role, r.Rol),
            new("api_token", r.Token)
        };
        var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(new ClaimsPrincipal(identidad));

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult Denegado() => View();
}

// ============ Panel principal + RF-08 capacidad ============
[Authorize]
public class HomeController(ApiClient api) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await api.CapacidadAsync());

    [HttpPost, Authorize(Roles = Rol.Admin)]
    public async Task<IActionResult> Capacidad(int capacidadTotal)
    {
        await Intentar(() => api.ActualizarCapacidadAsync(capacidadTotal), "Capacidad actualizada.");
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Error() => Content("Ocurrió un error inesperado.");
}

// ============ RF-01 / RF-02 / RF-10: animales ============
[Authorize]
public class AnimalesController(ApiClient api, IWebHostEnvironment env) : ControladorBase
{
    public async Task<IActionResult> Index(string? estado)
    {
        ViewBag.Estado = estado;
        return View(await api.ListarAnimalesAsync(estado));
    }

    public async Task<IActionResult> Detalle(int id) => View(await api.ObtenerAnimalAsync(id));

    [Authorize(Roles = Rol.Admin)]
    public IActionResult Crear() => View();

    [HttpPost, Authorize(Roles = Rol.Admin)]
    public async Task<IActionResult> Crear(string nombre, string especie, string raza, int edad,
                                           string estadoSalud, DateTime fechaIngreso, IFormFile? foto)
    {
        string? url = null;
        if (foto is { Length: > 0 })
        {
            var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
            if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
            {
                var carpeta = Path.Combine(env.WebRootPath, "uploads");
                Directory.CreateDirectory(carpeta);
                var nombreArchivo = $"{Guid.NewGuid()}{ext}";
                await using var fs = System.IO.File.Create(Path.Combine(carpeta, nombreArchivo));
                await foto.CopyToAsync(fs);
                url = $"/uploads/{nombreArchivo}";
            }
        }

        var ok = await Intentar(() => api.CrearAnimalAsync(
            new CrearAnimalRequest(nombre, especie, raza, edad, estadoSalud, fechaIngreso, url)),
            "Animal registrado correctamente.");
        return ok ? RedirectToAction(nameof(Index)) : View();
    }

    [HttpPost, Authorize(Roles = Rol.Admin)]
    public async Task<IActionResult> ProgramarVacuna(int id, string nombre, DateTime fechaProgramada)
    {
        await Intentar(() => api.ProgramarVacunaAsync(id, new ProgramarVacunaRequest(nombre, fechaProgramada)), "Vacuna programada.");
        return RedirectToAction(nameof(Detalle), new { id });
    }
}

// ============ RF-03: adoptantes ============
[Authorize(Roles = Rol.Admin)]
public class AdoptantesController(ApiClient api) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await api.ListarAdoptantesAsync());

    public IActionResult Crear() => View();

    [HttpPost]
    public async Task<IActionResult> Crear(string nombreCompleto, string tipoDocumento, string numeroDocumento,
                                           string contacto, string correo, string direccion)
    {
        var ok = await Intentar(() => api.CrearAdoptanteAsync(
            new CrearAdoptanteRequest(nombreCompleto, tipoDocumento, numeroDocumento, contacto, correo, direccion)),
            "Adoptante registrado correctamente.");
        return ok ? RedirectToAction(nameof(Index)) : View();
    }
}

// ============ RF-04 / RF-05 / RF-09: solicitudes de adopción ============
[Authorize(Roles = Rol.Admin)]
public class AdopcionesController(ApiClient api) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await api.ListarAdopcionesAsync());

    public async Task<IActionResult> Iniciar(int? animalId)
    {
        var animales = (await api.ListarAnimalesAsync("Disponible"));
        var adoptantes = await api.ListarAdoptantesAsync();
        return View(new IniciarVm(animales, adoptantes, animalId));
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(int animalId, int adoptanteId)
    {
        var ok = await Intentar(() => api.IniciarAdopcionAsync(new IniciarAdopcionRequest(animalId, adoptanteId)),
            "Proceso de adopción iniciado: la solicitud quedó en evaluación.");
        return ok ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Iniciar), new { animalId });
    }

    [HttpPost]
    public async Task<IActionResult> Evaluar(int id, bool apto, string? observaciones)
    {
        await Intentar(() => api.EvaluarAsync(id, new EvaluarRequest(apto, observaciones)),
            apto ? "Evaluación registrada: Apto." : "Evaluación registrada: No Apto.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Aprobar(int id, string? observaciones)
    {
        await Intentar(() => api.AprobarAsync(id, new DecisionRequest(observaciones)), "Adopción aprobada.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Rechazar(int id, string? observaciones)
    {
        await Intentar(() => api.RechazarAsync(id, new DecisionRequest(observaciones)), "Adopción rechazada; el animal volvió a 'Disponible'.");
        return RedirectToAction(nameof(Index));
    }
}

// ============ RF-06: seguimiento post-adopción ============
[Authorize(Roles = Rol.Admin)]
public class SeguimientosController(ApiClient api) : ControladorBase
{
    public async Task<IActionResult> Index()
    {
        var seguimientos = await api.ListarSeguimientosAsync();
        var aprobadas = (await api.ListarAdopcionesAsync()).Where(a => a.Estado == "Aprobada").ToList();
        return View(new SeguimientosVm(seguimientos, aprobadas));
    }

    [HttpPost]
    public async Task<IActionResult> Programar(int adopcionId, DateTime fechaControl)
    {
        await Intentar(() => api.ProgramarSeguimientoAsync(new ProgramarSeguimientoRequest(adopcionId, fechaControl)), "Control programado.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Resultado(int id, string resultado)
    {
        await Intentar(() => api.RegistrarResultadoAsync(id, new RegistrarResultadoRequest(resultado)), "Resultado del control registrado.");
        return RedirectToAction(nameof(Index));
    }
}

// ============ RF-07: notificaciones ============
[Authorize(Roles = Rol.Admin)]
public class NotificacionesController(ApiClient api) : ControladorBase
{
    public async Task<IActionResult> Index() => View(await api.ListarNotificacionesAsync());

    [HttpPost]
    public async Task<IActionResult> Procesar()
    {
        try
        {
            var r = await api.ProcesarRecordatoriosAsync();
            TempData["Ok"] = r.Enviados > 0 ? $"Se generaron {r.Enviados} recordatorio(s)." : "No hay vacunas ni controles próximos por notificar.";
        }
        catch (ApiException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}

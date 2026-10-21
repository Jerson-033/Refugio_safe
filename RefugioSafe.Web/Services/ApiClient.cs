using System.Net;
using System.Net.Http.Headers;
using RefugioSafe.Contracts;

namespace RefugioSafe.Web.Services;

public class ApiException(string mensaje) : Exception(mensaje);

/// <summary>Agrega el JWT (guardado como claim en la cookie) a cada llamada hacia la API.</summary>
public class TokenHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = accessor.HttpContext?.User.FindFirst("api_token")?.Value;
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, ct);
    }
}

public class ApiClient(HttpClient http)
{
    // ---------- helpers ----------
    private static async Task Validar(HttpResponseMessage r)
    {
        if (r.IsSuccessStatusCode) return;
        if (r.StatusCode == HttpStatusCode.Unauthorized) throw new ApiException("Su sesión expiró. Vuelva a iniciar sesión.");
        if (r.StatusCode == HttpStatusCode.Forbidden) throw new ApiException("No tiene permisos para esta acción.");
        string mensaje;
        try { mensaje = (await r.Content.ReadFromJsonAsync<ErrorDto>())?.Mensaje ?? $"Error {(int)r.StatusCode}"; }
        catch { mensaje = $"Error {(int)r.StatusCode}"; }
        throw new ApiException(mensaje);
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var r = await http.GetAsync(url);
        await Validar(r);
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> PostAsync<T>(string url, object? cuerpo)
    {
        var r = await http.PostAsJsonAsync(url, cuerpo);
        await Validar(r);
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task PostAsync(string url, object? cuerpo)
    {
        var r = await http.PostAsJsonAsync(url, cuerpo);
        await Validar(r);
    }

    // ---------- autenticación ----------
    public async Task<LoginResponse?> LoginAsync(string usuario, string password)
    {
        var r = await http.PostAsJsonAsync("api/auth/login", new LoginRequest(usuario, password));
        if (!r.IsSuccessStatusCode) return null;
        return await r.Content.ReadFromJsonAsync<LoginResponse>();
    }

    // ---------- capacidad ----------
    public Task<CapacidadDto> CapacidadAsync() => GetAsync<CapacidadDto>("api/capacidad");

    public async Task ActualizarCapacidadAsync(int total)
    {
        var r = await http.PutAsJsonAsync("api/capacidad", new ActualizarCapacidadRequest(total));
        await Validar(r);
    }

    // ---------- animales ----------
    public Task<List<AnimalDto>> ListarAnimalesAsync(string? estado) =>
        GetAsync<List<AnimalDto>>(string.IsNullOrEmpty(estado) ? "api/animales" : $"api/animales?estado={estado}");

    public Task<AnimalDetalleDto> ObtenerAnimalAsync(int id) => GetAsync<AnimalDetalleDto>($"api/animales/{id}");
    public Task CrearAnimalAsync(CrearAnimalRequest r) => PostAsync("api/animales", r);
    public Task ProgramarVacunaAsync(int animalId, ProgramarVacunaRequest r) => PostAsync($"api/animales/{animalId}/vacunas", r);

    // ---------- adoptantes ----------
    public Task<List<AdoptanteDto>> ListarAdoptantesAsync() => GetAsync<List<AdoptanteDto>>("api/adoptantes");
    public Task CrearAdoptanteAsync(CrearAdoptanteRequest r) => PostAsync("api/adoptantes", r);

    // ---------- adopciones ----------
    public Task<List<AdopcionDto>> ListarAdopcionesAsync() => GetAsync<List<AdopcionDto>>("api/adopciones");
    public Task IniciarAdopcionAsync(IniciarAdopcionRequest r) => PostAsync("api/adopciones", r);
    public Task EvaluarAsync(int id, EvaluarRequest r) => PostAsync($"api/adopciones/{id}/evaluar", r);
    public Task AprobarAsync(int id, DecisionRequest r) => PostAsync($"api/adopciones/{id}/aprobar", r);
    public Task RechazarAsync(int id, DecisionRequest r) => PostAsync($"api/adopciones/{id}/rechazar", r);

    // ---------- seguimientos ----------
    public Task<List<SeguimientoDto>> ListarSeguimientosAsync() => GetAsync<List<SeguimientoDto>>("api/seguimientos");
    public Task ProgramarSeguimientoAsync(ProgramarSeguimientoRequest r) => PostAsync("api/seguimientos", r);
    public Task RegistrarResultadoAsync(int id, RegistrarResultadoRequest r) => PostAsync($"api/seguimientos/{id}/resultado", r);

    // ---------- notificaciones ----------
    public Task<List<NotificacionDto>> ListarNotificacionesAsync() => GetAsync<List<NotificacionDto>>("api/notificaciones");
    public Task<ProcesarResultadoDto> ProcesarRecordatoriosAsync() => PostAsync<ProcesarResultadoDto>("api/notificaciones/procesar", null);
}

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RefugioSafe.Api.Application;
using RefugioSafe.Api.Domain;
using RefugioSafe.Api.Infrastructure;
using RefugioSafe.Contracts;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---------- Base de datos (SQL Server + EF Core) ----------
builder.Services.AddDbContext<RefugioDbContext>(o => o.UseSqlServer(config.GetConnectionString("RefugioDb")));

// ---------- Opciones ----------
builder.Services.Configure<JwtOptions>(config.GetSection("Jwt"));
builder.Services.Configure<RecordatoriosOptions>(config.GetSection("Recordatorios"));
builder.Services.Configure<SmtpOptions>(config.GetSection("Notificaciones:Smtp"));
builder.Services.AddSingleton(TimeProvider.System);

// ---------- DIP: abstracciones -> implementaciones ----------
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAnimalRepository, AnimalRepository>();
builder.Services.AddScoped<IAdoptanteRepository, AdoptanteRepository>();
builder.Services.AddScoped<IAdopcionRepository, AdopcionRepository>();
builder.Services.AddScoped<ISeguimientoRepository, SeguimientoRepository>();
builder.Services.AddScoped<INotificacionRepository, NotificacionRepository>();
builder.Services.AddScoped<IRecordatorioRepository, RecordatorioRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRefugioRepository, RefugioRepository>();

// ---------- Servicios de aplicación ----------
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AnimalService>();
builder.Services.AddScoped<AdoptanteService>();
builder.Services.AddScoped<AdopcionService>();
builder.Services.AddScoped<CapacidadService>();
builder.Services.AddScoped<SeguimientoService>();
builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<RecordatorioService>();

// ---------- PATRÓN OBSERVER: receptores registrados ----------
builder.Services.AddScoped<IAdopcionObserver, CapacidadObserver>();
builder.Services.AddScoped<IAdopcionObserver, SeguimientoObserver>();
builder.Services.AddScoped<IAdopcionObserver, NotificacionObserver>();

// ---------- PATRÓN STRATEGY: canal de notificación según configuración ----------
if (string.Equals(config["Notificaciones:Canal"], "Email", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddScoped<INotificador, EmailNotificador>();
else
    builder.Services.AddScoped<INotificador, NotificadorMock>();

// ---------- Contenedor "Background Service" ----------
builder.Services.AddHostedService<RecordatoriosHostedService>();

// ---------- Autenticación JWT + autorización por rol ----------
var jwt = config.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Emisor,
        ValidateAudience = true, ValidAudience = jwt.Emisor,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
        ValidateLifetime = true
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

// Traduce reglas de negocio a respuestas HTTP legibles para la web
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (ReglaNegocioException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new ErrorDto(ex.Message));
    }
    catch (KeyNotFoundException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        await ctx.Response.WriteAsJsonAsync(new ErrorDto(ex.Message));
    }
});

using (var scope = app.Services.CreateScope())
    DataSeeder.Sembrar(scope.ServiceProvider.GetRequiredService<RefugioDbContext>());

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

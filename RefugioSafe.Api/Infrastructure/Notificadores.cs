using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RefugioSafe.Api.Application;

namespace RefugioSafe.Api.Infrastructure;

// ===================== PATRÓN STRATEGY: estrategias concretas =====================

/// <summary>Envío real por SMTP. Se activa con Notificaciones:Canal = "Email".</summary>
public class EmailNotificador(IOptions<SmtpOptions> opciones) : INotificador
{
    public async Task<bool> EnviarAsync(string destinatario, string asunto, string mensaje)
    {
        var o = opciones.Value;
        using var cliente = new SmtpClient(o.Host, o.Puerto)
        {
            EnableSsl = o.UsarSsl,
            Credentials = new NetworkCredential(o.Usuario, o.Clave)
        };
        using var correo = new MailMessage(o.Remitente, destinatario, asunto, mensaje);
        await cliente.SendMailAsync(correo);
        return true;
    }
}

/// <summary>Estrategia simulada para desarrollo y pruebas: no envía nada, solo registra en el log.</summary>
public class NotificadorMock(ILogger<NotificadorMock> log) : INotificador
{
    public Task<bool> EnviarAsync(string destinatario, string asunto, string mensaje)
    {
        log.LogInformation("[SIMULADO] Correo a {Destinatario} | {Asunto} | {Mensaje}", destinatario, asunto, mensaje);
        return Task.FromResult(true);
    }
}

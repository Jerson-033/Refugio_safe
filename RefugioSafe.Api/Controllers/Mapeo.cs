using RefugioSafe.Api.Domain;
using RefugioSafe.Contracts;

namespace RefugioSafe.Api.Controllers;

public static class Mapeo
{
    public static AnimalDto ToDto(this Animal a) =>
        new(a.Id, a.Nombre, a.Especie, a.Raza, a.Edad, a.EstadoSalud, a.Estado.ToString(), a.FechaIngreso, a.FotoUrl);

    public static VacunaDto ToDto(this Vacuna v) => new(v.Id, v.Nombre, v.FechaProgramada, v.FechaAplicada);

    public static AdoptanteDto ToDto(this Adoptante a) =>
        new(a.Id, a.NombreCompleto, a.TipoDocumento, a.NumeroDocumento, a.Contacto, a.Correo, a.Direccion);

    public static AdopcionDto ToDto(this Adopcion a) =>
        new(a.Id, a.AnimalId, $"{a.Animal.Nombre} - {a.Animal.Especie} {a.Animal.Raza} - {a.Animal.Edad} año(s)",
            a.AdoptanteId, $"{a.Adoptante.NombreCompleto} - {a.Adoptante.TipoDocumento} {a.Adoptante.NumeroDocumento}",
            a.Estado.ToString(), a.FechaSolicitud, a.FechaAdopcion, a.Observaciones);

    public static SeguimientoDto ToDto(this Seguimiento s) =>
        new(s.Id, s.AdopcionId, s.Adopcion.Animal.Nombre, s.Adopcion.Adoptante.NombreCompleto,
            s.FechaControl, s.Estado.ToString(), s.Resultado);

    public static NotificacionDto ToDto(this Notificacion n) =>
        new(n.Id, n.Animal.Nombre, n.Adoptante.NombreCompleto, n.Tipo.ToString(), n.EnviadaPor, n.Fecha, n.Estado.ToString());
}

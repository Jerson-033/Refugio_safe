using System.Text.RegularExpressions;
using RefugioSafe.Contracts;

namespace RefugioSafe.Web.Models;

public record IniciarVm(List<AnimalDto> Animales, List<AdoptanteDto> Adoptantes, int? AnimalId);
public record SeguimientosVm(List<SeguimientoDto> Seguimientos, List<AdopcionDto> Aprobadas);

public static class Rol { public const string Admin = "Administrador"; }

public static class Fmt
{
    // "EnProceso" -> "En proceso", "EvaluadaNoApta" -> "Evaluada no apta"
    public static string Estado(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ").ToLowerInvariant().Replace("adopcion", "adopción");
        return char.ToUpperInvariant(t[0]) + t[1..];
    }
}

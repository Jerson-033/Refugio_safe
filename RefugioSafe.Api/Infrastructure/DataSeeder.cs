using Microsoft.AspNetCore.Identity;
using RefugioSafe.Api.Domain;

namespace RefugioSafe.Api.Infrastructure;

/// <summary>Crea la base de datos y datos de demostración basados en el prototipo Figma.</summary>
public static class DataSeeder
{
    public static void Sembrar(RefugioDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Usuarios.Any()) return;

        var hasher = new PasswordHasher<Usuario>();
        var admin = new Usuario { NombreUsuario = "admin", Rol = RolUsuario.Administrador };
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123*");
        var personal = new Usuario { NombreUsuario = "personal", Rol = RolUsuario.PersonalRefugio };
        personal.PasswordHash = hasher.HashPassword(personal, "Personal123*");
        db.Usuarios.AddRange(admin, personal);

        db.Refugios.Add(new Refugio { Nombre = "Refugio Safe", Ubicacion = "Medellín, Antioquia", CapacidadTotal = 30 });

        var hoy = DateTime.Today;
        var tobi = new Animal { Nombre = "Tobi", Especie = "Perro", Raza = "Schnauzer", Edad = 5, EstadoSalud = "Vacunado", FechaIngreso = hoy.AddDays(-40) };
        var tom = new Animal { Nombre = "Tom", Especie = "Gato", Raza = "Persa", Edad = 2, EstadoSalud = "Vacunado", FechaIngreso = hoy.AddDays(-30) };
        var rocky = new Animal { Nombre = "Rocky", Especie = "Perro", Raza = "Labrador", Edad = 7, EstadoSalud = "Vacunado", FechaIngreso = hoy.AddDays(-25) };
        var luna = new Animal { Nombre = "Luna", Especie = "Gato", Raza = "Siamés", Edad = 1, EstadoSalud = "Saludable", FechaIngreso = hoy.AddDays(-10), Estado = EstadoAnimal.EnProceso };
        var rex = new Animal { Nombre = "Rex", Especie = "Perro", Raza = "Criollo", Edad = 4, EstadoSalud = "Vacunado", FechaIngreso = hoy.AddDays(-90), Estado = EstadoAnimal.Adoptado };
        rex.Vacunas.Add(new Vacuna { Nombre = "Rabia", FechaProgramada = hoy.AddDays(2) });   // dispara un recordatorio en la demo
        db.Animales.AddRange(tobi, tom, rocky, luna, rex);

        // Nombres de adoptantes tomados del prototipo; los documentos (salvo el de Camila) son de ejemplo
        var camila = new Adoptante { NombreCompleto = "Camila Restrepo", TipoDocumento = "CC", NumeroDocumento = "1254789635", Contacto = "3001234567", Correo = "camila.restrepo@example.com", Direccion = "Calle 10 # 20-30" };
        var jorge = new Adoptante { NombreCompleto = "Jorge Pineda", TipoDocumento = "CC", NumeroDocumento = "1017654321", Contacto = "3012345678", Correo = "jorge.pineda@example.com", Direccion = "Carrera 45 # 12-08" };
        var vanesa = new Adoptante { NombreCompleto = "Vanesa Salazar", TipoDocumento = "CC", NumeroDocumento = "43987654", Contacto = "3023456789", Correo = "vanesa.salazar@example.com", Direccion = "Avenida 80 # 33-15" };
        db.Adoptantes.AddRange(camila, jorge, vanesa);

        // Solicitud #1 en evaluación (Luna / Camila) y adopción ya aprobada (Rex / Vanesa)
        db.Adopciones.Add(new Adopcion { Animal = luna, Adoptante = camila, FechaSolicitud = hoy.AddDays(-1), Estado = EstadoAdopcion.Iniciada });
        var adopcionRex = new Adopcion { Animal = rex, Adoptante = vanesa, FechaSolicitud = hoy.AddDays(-22), FechaAdopcion = hoy.AddDays(-20), Estado = EstadoAdopcion.Aprobada, Observaciones = "Hogar con patio" };
        db.Adopciones.Add(adopcionRex);
        db.Seguimientos.Add(new Seguimiento { Adopcion = adopcionRex, FechaControl = hoy.AddDays(10) });

        db.SaveChanges();
    }
}

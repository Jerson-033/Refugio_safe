using Microsoft.EntityFrameworkCore;
using RefugioSafe.Api.Application;
using RefugioSafe.Api.Domain;

namespace RefugioSafe.Api.Infrastructure;

public class AnimalRepository(RefugioDbContext db) : IAnimalRepository
{
    public Task<List<Animal>> ListarAsync(EstadoAnimal? estado)
    {
        var q = db.Animales.AsQueryable();
        if (estado.HasValue) q = q.Where(a => a.Estado == estado.Value);
        return q.OrderBy(a => a.Nombre).ToListAsync();
    }

    public Task<Animal?> ObtenerAsync(int id) =>
        db.Animales.Include(a => a.Vacunas).FirstOrDefaultAsync(a => a.Id == id);

    public Task<bool> ExisteDuplicadoAsync(string nombre, string especie, string raza, DateTime fechaIngreso) =>
        db.Animales.AnyAsync(a => a.Nombre == nombre && a.Especie == especie && a.Raza == raza && a.FechaIngreso == fechaIngreso);

    public async Task AgregarAsync(Animal animal) => await db.Animales.AddAsync(animal);

    // Alojados = Disponible + EnProceso (los adoptados ya salieron del refugio)
    public Task<int> ContarAlojadosAsync() =>
        db.Animales.CountAsync(a => a.Estado != EstadoAnimal.Adoptado);

    public Task<Dictionary<string, int>> ContarAlojadosPorEspecieAsync() =>
        db.Animales.Where(a => a.Estado != EstadoAnimal.Adoptado)
            .GroupBy(a => a.Especie)
            .Select(g => new { Especie = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Especie, x => x.Total);
}

public class AdoptanteRepository(RefugioDbContext db) : IAdoptanteRepository
{
    public Task<List<Adoptante>> ListarAsync() => db.Adoptantes.OrderBy(a => a.NombreCompleto).ToListAsync();
    public Task<Adoptante?> ObtenerAsync(int id) => db.Adoptantes.FirstOrDefaultAsync(a => a.Id == id);
    public Task<bool> ExisteDocumentoAsync(string numeroDocumento) => db.Adoptantes.AnyAsync(a => a.NumeroDocumento == numeroDocumento);
    public async Task AgregarAsync(Adoptante adoptante) => await db.Adoptantes.AddAsync(adoptante);
}

public class AdopcionRepository(RefugioDbContext db) : IAdopcionRepository
{
    private IQueryable<Adopcion> Consulta => db.Adopciones.Include(a => a.Animal).Include(a => a.Adoptante);

    public Task<List<Adopcion>> ListarAsync(EstadoAdopcion? estado)
    {
        var q = Consulta;
        if (estado.HasValue) q = q.Where(a => a.Estado == estado.Value);
        return q.OrderByDescending(a => a.Id).ToListAsync();
    }

    public Task<Adopcion?> ObtenerAsync(int id) => Consulta.FirstOrDefaultAsync(a => a.Id == id);

    public Task<Adopcion?> ObtenerAprobadaPorAnimalAsync(int animalId) =>
        Consulta.FirstOrDefaultAsync(a => a.AnimalId == animalId && a.Estado == EstadoAdopcion.Aprobada);

    public async Task AgregarAsync(Adopcion adopcion) => await db.Adopciones.AddAsync(adopcion);
}

public class SeguimientoRepository(RefugioDbContext db) : ISeguimientoRepository
{
    private IQueryable<Seguimiento> Consulta =>
        db.Seguimientos.Include(s => s.Adopcion).ThenInclude(a => a.Animal)
                       .Include(s => s.Adopcion).ThenInclude(a => a.Adoptante);

    public Task<List<Seguimiento>> ListarAsync() => Consulta.OrderBy(s => s.FechaControl).ToListAsync();
    public Task<Seguimiento?> ObtenerAsync(int id) => Consulta.FirstOrDefaultAsync(s => s.Id == id);
    public async Task AgregarAsync(Seguimiento seguimiento) => await db.Seguimientos.AddAsync(seguimiento);
}

public class NotificacionRepository(RefugioDbContext db) : INotificacionRepository
{
    public Task<List<Notificacion>> ListarAsync() =>
        db.Notificaciones.Include(n => n.Animal).Include(n => n.Adoptante).OrderByDescending(n => n.Fecha).ToListAsync();

    public async Task AgregarAsync(Notificacion notificacion) => await db.Notificaciones.AddAsync(notificacion);
}

public class RecordatorioRepository(RefugioDbContext db) : IRecordatorioRepository
{
    public Task<List<Vacuna>> VacunasPorNotificarAsync(DateTime limite) =>
        db.Vacunas.Include(v => v.Animal)
            .Where(v => v.FechaAplicada == null && !v.RecordatorioEnviado && v.FechaProgramada <= limite)
            .ToListAsync();

    public Task<List<Seguimiento>> SeguimientosPorNotificarAsync(DateTime limite) =>
        db.Seguimientos
            .Include(s => s.Adopcion).ThenInclude(a => a.Animal)
            .Include(s => s.Adopcion).ThenInclude(a => a.Adoptante)
            .Where(s => s.Estado == EstadoSeguimiento.Pendiente && !s.RecordatorioEnviado && s.FechaControl <= limite)
            .ToListAsync();
}

public class UsuarioRepository(RefugioDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
}

public class RefugioRepository(RefugioDbContext db) : IRefugioRepository
{
    public Task<Refugio> ObtenerAsync() => db.Refugios.FirstAsync();
}

using Microsoft.EntityFrameworkCore;
using RefugioSafe.Api.Application;
using RefugioSafe.Api.Domain;

namespace RefugioSafe.Api.Infrastructure;

public class RefugioDbContext(DbContextOptions<RefugioDbContext> options) : DbContext(options)
{
    public DbSet<Refugio> Refugios => Set<Refugio>();
    public DbSet<Animal> Animales => Set<Animal>();
    public DbSet<Vacuna> Vacunas => Set<Vacuna>();
    public DbSet<Adoptante> Adoptantes => Set<Adoptante>();
    public DbSet<Adopcion> Adopciones => Set<Adopcion>();
    public DbSet<Seguimiento> Seguimientos => Set<Seguimiento>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // RN-02 / RNF-03: documento único
        b.Entity<Adoptante>().HasIndex(a => a.NumeroDocumento).IsUnique();
        b.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

        // Enumeraciones guardadas como texto (más legibles en SQL Server)
        b.Entity<Animal>().Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Adopcion>().Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Seguimiento>().Property(s => s.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Notificacion>().Property(n => n.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Entity<Notificacion>().Property(n => n.Estado).HasConversion<string>().HasMaxLength(20);
        b.Entity<Usuario>().Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);

        // Evita rutas de borrado en cascada múltiples en SQL Server
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}

public class UnitOfWork(RefugioDbContext db) : IUnitOfWork
{
    public Task GuardarCambiosAsync() => db.SaveChangesAsync();
}

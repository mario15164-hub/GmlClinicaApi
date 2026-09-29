using Microsoft.EntityFrameworkCore;
using GmlClinicaApi.Models;

namespace GmlClinicaApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Utilizadores> Utilizadores { get; set; }
        public DbSet<Pacientes> Pacientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Utilizadores>(entity =>
            {
                entity.ToTable("utilizadores");
                entity.HasKey(e => e.Id);
            });

            modelBuilder.Entity<Pacientes>(entity =>
            {
                entity.ToTable("pacientes");
                entity.HasKey(e => e.Id);
            });
        }
    }
}

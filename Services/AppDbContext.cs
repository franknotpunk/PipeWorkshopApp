using Microsoft.EntityFrameworkCore;
using PipeWorkshopApp.Models;

namespace PipeWorkshopApp.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<PipeData> Pipes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Задайте строку подключения к вашей базе данных PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=PipeWorkshopDb;Username=postgres;Password=postgres");
        }
    }
}

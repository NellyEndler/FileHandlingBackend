using FileHandlingBackend.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FileHandlingBackend.Context
{
    public class FileHandlingContext(DbContextOptions<FileHandlingContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<LoginCredential> LoginCredentials { get; set; }
        public DbSet<Scene> Scenes { get; set; }
        public DbSet<Project> Projects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

    }
}

using BibliotecaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Livro>(e =>
        {
            e.ToTable("livros");
            e.HasKey(l => l.Id);
            e.Property(l => l.Id).HasColumnName("id");
            e.Property(l => l.Titulo).HasColumnName("titulo").HasMaxLength(200).IsRequired();
            e.Property(l => l.Autor).HasColumnName("autor").HasMaxLength(150).IsRequired();
            e.Property(l => l.AnoPublicacao).HasColumnName("ano_publicacao");
            e.Property(l => l.Genero).HasColumnName("genero").HasMaxLength(80);
            e.Property(l => l.CriadoEm).HasColumnName("criado_em");
            e.Property(l => l.AtualizadoEm).HasColumnName("atualizado_em");
        });
    }
}

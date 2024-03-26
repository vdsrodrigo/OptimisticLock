using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public class AcumuloContext : DbContext
{
    public AcumuloContext(DbContextOptions<AcumuloContext> options)
        : base(options) { }

    public DbSet<ShellRepository> AcumuladorShell => Set<ShellRepository>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ShellRepository>()
        .Property(p => p.RowVersion)
        .IsConcurrencyToken();
    }
}

[Table("acumuladorshell")]
public class ShellRepository : DbContext
{

    [System.ComponentModel.DataAnnotations.Key]
    [Column("id")]
    public Guid Id{get;set;}

    [Column("first_name")]
    public string Nome { get; set; }

    [Column("value")]
    public int Valor { get; set; }

    [Column("row_version")]
    public int RowVersion { get; set; } // Coluna para o optimistic lock
}
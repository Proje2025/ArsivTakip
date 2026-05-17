using Microsoft.EntityFrameworkCore;
using BirtanaArsivTakip.Models;

namespace BirtanaArsivTakip.Data;

public class ArsivDbContext : DbContext
{
    public DbSet<Klasor> Klasorler { get; set; } = null!;
    public DbSet<Evrak> Evraklar { get; set; } = null!;

    public ArsivDbContext(DbContextOptions<ArsivDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Klasor>()
            .HasOne(k => k.UstKlasor)
            .WithMany(k => k.AltKlasorler)
            .HasForeignKey(k => k.UstKlasorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Evrak>()
            .HasOne(e => e.Klasor)
            .WithMany(k => k.Evraklar)
            .HasForeignKey(e => e.KlasorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Evrak>()
            .HasIndex(e => e.Sayi);

        modelBuilder.Entity<Evrak>()
            .HasIndex(e => e.Konu);
    }
}
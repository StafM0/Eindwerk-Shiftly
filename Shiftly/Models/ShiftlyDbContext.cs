using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Shiftly.Models;

public partial class ShiftlyDbContext : DbContext
{
    public ShiftlyDbContext()
    {
    }

    public ShiftlyDbContext(DbContextOptions<ShiftlyDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Abonnement> Abonnements { get; set; }

    public virtual DbSet<Afdeling> Afdelings { get; set; }

    public virtual DbSet<Gebruiker> Gebruikers { get; set; }

    public virtual DbSet<Gebruikerabonnement> Gebruikerabonnements { get; set; }

    public virtual DbSet<Gebruikerafdeling> Gebruikerafdelings { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Shiftly> Shiftlies { get; set; }

    public virtual DbSet<Werkplek> Werkpleks { get; set; }

    public virtual DbSet<Wishlistitem> Wishlistitems { get; set; }

    // Connection string is configured via DI in Program.cs – nothing to do here.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Abonnement>(entity =>
        {
            entity.HasKey(e => e.IdAbonnement).HasName("PRIMARY");

            entity.ToTable("abonnement");

            entity.HasIndex(e => e.NaamAbonnement, "NaamAbonnement").IsUnique();

            entity.Property(e => e.BedragAbonnement).HasPrecision(10, 2);
            entity.Property(e => e.IsActief)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.OmschrijvingAbonnement).HasColumnType("text");
        });

        modelBuilder.Entity<Afdeling>(entity =>
        {
            entity.HasKey(e => e.IdAfdeling).HasName("PRIMARY");

            entity.ToTable("afdeling");

            entity.HasIndex(e => new { e.FkWerkplek, e.AfdelingNaam }, "FkWerkplek").IsUnique();

            entity.HasOne(d => d.FkWerkplekNavigation).WithMany(p => p.Afdelings)
                .HasForeignKey(d => d.FkWerkplek)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("afdeling_ibfk_1");
        });

        modelBuilder.Entity<Gebruiker>(entity =>
        {
            entity.HasKey(e => e.IdGebruiker).HasName("PRIMARY");

            entity.ToTable("gebruiker");

            entity.HasIndex(e => e.EmailGebruiker, "EmailGebruiker").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActief)
                .IsRequired()
                .HasDefaultValueSql("'1'");
            entity.Property(e => e.NaamGebruiker).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime");
            entity.Property(e => e.VoorNaamGebruiker).HasMaxLength(255);
            entity.Property(e => e.WachtwoordGebruiker).HasMaxLength(26);
        });

        modelBuilder.Entity<Gebruikerabonnement>(entity =>
        {
            entity.HasKey(e => new { e.FkGebruiker, e.FkAbonnement, e.PeriodeStart })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0, 0 });

            entity.ToTable("gebruikerabonnement");

            entity.HasIndex(e => e.FkAbonnement, "FkAbonnement");

            entity.HasOne(d => d.FkAbonnementNavigation).WithMany(p => p.Gebruikerabonnements)
                .HasForeignKey(d => d.FkAbonnement)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gebruikerabonnement_ibfk_2");

            entity.HasOne(d => d.FkGebruikerNavigation).WithMany(p => p.Gebruikerabonnements)
                .HasForeignKey(d => d.FkGebruiker)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gebruikerabonnement_ibfk_1");
        });

        modelBuilder.Entity<Gebruikerafdeling>(entity =>
        {
            entity.HasKey(e => e.IdGebruikerAfdeling).HasName("PRIMARY");

            entity.ToTable("gebruikerafdeling");

            entity.HasIndex(e => e.FkAfdeling, "FkAfdeling");

            entity.HasIndex(e => new { e.FkGebruiker, e.FkAfdeling }, "FkGebruiker").IsUnique();

            entity.Property(e => e.Uurloon).HasPrecision(10, 2);

            entity.HasOne(d => d.FkAfdelingNavigation).WithMany(p => p.Gebruikerafdelings)
                .HasForeignKey(d => d.FkAfdeling)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gebruikerafdeling_ibfk_2");

            entity.HasOne(d => d.FkGebruikerNavigation).WithMany(p => p.Gebruikerafdelings)
                .HasForeignKey(d => d.FkGebruiker)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("gebruikerafdeling_ibfk_1");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.IdShift).HasName("PRIMARY");

            entity.ToTable("shift");

            entity.HasIndex(e => e.FkGebruikerAfdeling, "FkGebruikerAfdeling");

            entity.Property(e => e.EindDateTime).HasColumnType("datetime");
            entity.Property(e => e.Functie).HasMaxLength(255);
            entity.Property(e => e.Opmerking).HasMaxLength(255);
            entity.Property(e => e.StartDateTime).HasColumnType("datetime");

            entity.HasOne(d => d.FkGebruikerAbbonomentNavigation).WithMany(p => p.Shifts)
                .HasForeignKey(d => d.FkGebruikerAfdeling)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shift_ibfk_1");
        });

        modelBuilder.Entity<Shiftly>(entity =>
        {
            entity.HasKey(e => e.FkGebruiker).HasName("PRIMARY");

            entity.ToTable("shiftly");

            entity.Property(e => e.FkGebruiker).ValueGeneratedNever();
            entity.Property(e => e.MaximumStudentUren).HasDefaultValueSql("'650'");

            entity.HasOne(d => d.FkGebruikerNavigation).WithOne(p => p.Shiftly)
                .HasForeignKey<Shiftly>(d => d.FkGebruiker)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("shiftly_ibfk_1");
        });

        modelBuilder.Entity<Werkplek>(entity =>
        {
            entity.HasKey(e => e.IdWerkplek).HasName("PRIMARY");

            entity.ToTable("werkplek");

            entity.HasIndex(e => new { e.Postcode, e.Gemeente, e.StraatNr }, "Postcode").IsUnique();

            entity.Property(e => e.Naam).HasMaxLength(255);
            entity.Property(e => e.Postcode)
                .HasMaxLength(4)
                .IsFixedLength();
        });

        modelBuilder.Entity<Wishlistitem>(entity =>
        {
            entity.HasKey(e => e.IdWishListItem).HasName("PRIMARY");

            entity.ToTable("wishlistitem");

            entity.HasIndex(e => e.FkGebruiker, "FkGebruiker");

            entity.Property(e => e.ItemLink).HasMaxLength(255);
            entity.Property(e => e.ItemNaam).HasMaxLength(255);
            entity.Property(e => e.ItemOmschrijving).HasColumnType("text");
            entity.Property(e => e.ItemPrijs).HasPrecision(10, 2);

            entity.HasOne(d => d.FkGebruikerNavigation).WithMany(p => p.Wishlistitems)
                .HasForeignKey(d => d.FkGebruiker)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("wishlistitem_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

using GeneticSolverLibrary;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace GeneticSolverDatabase
{
    public class Record
    {
        public int RecordId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreationDate { get; set; }

        public int GenSolverId { get; set; }
        public GenSolver GenSolver { get; set; } = null!;
    }

    public class GeneticAlgorithmContext : DbContext
    {
        public DbSet<Figure> Figures { get; set; } = null!;
        public DbSet<Grid> Grids { get; set; } = null!;
        public DbSet<GenSolver> GenSolvers { get; set; } = null!;
        public DbSet<Record> Records { get; set; } = null!;

        public GeneticAlgorithmContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=GeneticAlgorithm.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Figure>()
                .HasOne(f => f.Grid)
                .WithMany(g => g.Figures)
                .HasForeignKey(f => f.GridId);

            modelBuilder.Entity<Grid>()
                .HasOne(g => g.GenSolver)
                .WithMany(gs => gs.GridsForDb)
                .HasForeignKey(g => g.GenSolverId);

            modelBuilder.Entity<Record>()
                .HasOne(r => r.GenSolver)
                .WithOne()
                .HasForeignKey<Record>(r => r.GenSolverId);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MILK_PRODUCTS.Models;

public partial class DbmsContext : DbContext
{
    public DbmsContext()
    {
    }

    public DbmsContext(DbContextOptions<DbmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Budget> Budgets { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Finproduct> Finproducts { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Position> Positions { get; set; }

    public virtual DbSet<Production> Productions { get; set; }

    public virtual DbSet<PurchaseOfRawMaterial> PurchaseOfRawMaterials { get; set; }

    public virtual DbSet<RawMaterial> RawMaterials { get; set; }

    public virtual DbSet<SaleOfProduct> SaleOfProducts { get; set; }

    public virtual DbSet<Unit> Units { get; set; }
    public virtual DbSet<Salaries> Salaries { get; set; }
    public virtual DbSet<Credit> Credits { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.BudgetAmount).HasColumnType("money");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(50);
            entity.Property(e => e.Fullname).HasMaxLength(50);
            entity.Property(e => e.Salary).HasColumnType("money");
            entity.Property(e => e.Telephone).HasMaxLength(50);

            entity.HasOne(d => d.PositionNavigation).WithMany(p => p.Employees)
                .HasForeignKey(d => d.Position)
                .HasConstraintName("FK_Employees_Positions");
        });

        modelBuilder.Entity<Finproduct>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Sum).HasColumnType("money");

            entity.HasOne(d => d.UnitNavigation).WithMany(p => p.Finproducts)
                .HasForeignKey(d => d.Unit)
                .HasConstraintName("FK_Finproducts_Units");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.Product)
                .HasConstraintName("FK_Ingredients_Finproducts");

            entity.HasOne(d => d.RawMaterialsNavigation).WithMany(p => p.Ingredients)
                .HasForeignKey(d => d.RawMaterials)
                .HasConstraintName("FK_Ingredients_RawMaterials");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Position1)
                .HasMaxLength(50)
                .HasColumnName("Position");
        });

        modelBuilder.Entity<Production>(entity =>
        {
            entity.ToTable("Production");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("smalldatetime");

            entity.HasOne(d => d.EmployeeNavigation).WithMany(p => p.Productions)
                .HasForeignKey(d => d.Employee)
                .HasConstraintName("FK_Production_Employees");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.Productions)
                .HasForeignKey(d => d.Product)
                .HasConstraintName("FK_Production_Finproducts");
        });

        modelBuilder.Entity<PurchaseOfRawMaterial>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("smalldatetime");
            entity.Property(e => e.Sum).HasColumnType("money");

            entity.HasOne(d => d.EmployeeNavigation).WithMany(p => p.PurchaseOfRawMaterials)
                .HasForeignKey(d => d.Employee)
                .HasConstraintName("FK_PurchaseOfRawMaterials_Employees");

            entity.HasOne(d => d.RawMaterialsNavigation).WithMany(p => p.PurchaseOfRawMaterials)
                .HasForeignKey(d => d.RawMaterials)
                .HasConstraintName("FK_PurchaseOfRawMaterials_RawMaterials");
        });

        modelBuilder.Entity<RawMaterial>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Sum).HasColumnType("money");

            entity.HasOne(d => d.UnitNavigation).WithMany(p => p.RawMaterials)
                .HasForeignKey(d => d.Unit)
                .HasConstraintName("FK_RawMaterials_Units");
        });

        modelBuilder.Entity<SaleOfProduct>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Date).HasColumnType("smalldatetime");
            entity.Property(e => e.Sum).HasColumnType("money");

            entity.HasOne(d => d.EmployeeNavigation).WithMany(p => p.SaleOfProducts)
                .HasForeignKey(d => d.Employee)
                .HasConstraintName("FK_SaleOfProducts_Employees");

            entity.HasOne(d => d.ProductNavigation).WithMany(p => p.SaleOfProducts)
                .HasForeignKey(d => d.Product)
                .HasConstraintName("FK_SaleOfProducts_Finproducts");
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Salaries>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Bonus).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.EmployeeNavigation).WithMany(p => p.Salaries)
                .HasForeignKey(d => d.EmployeeID)
                .HasConstraintName("FK_Salaries_Employees");
        });

        modelBuilder.Entity<Credit>(entity =>
        {
            entity.ToTable("Credit");
            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CreditExpirationDate)
                  .HasColumnName("Credit_expiration_date")
                  .HasComputedColumnSql("dateadd(year,[Years],[DataOfReceive])", stored: true);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("ID");

            entity.HasOne(d => d.CreditNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CreditID)
                .HasConstraintName("FK_Payments_Credit");
        });
        // Настройка связей
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.ID).HasColumnName("ID");

            entity.HasOne(u => u.Role)
            .WithMany(r => r.Users)
                 .HasForeignKey(u => u.RoleId);
        }
        );

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

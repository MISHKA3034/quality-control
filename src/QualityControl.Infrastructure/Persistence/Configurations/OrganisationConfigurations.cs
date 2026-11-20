using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityControl.Domain.Entities;

namespace QualityControl.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Code).HasMaxLength(30).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(1000);

        builder.HasIndex(d => d.Code).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Department.Manager and Employee.Department form a cycle. Both ends are
        // Restrict/NoAction: SQL Server rejects multiple cascade paths, and neither
        // record should disappear because the other did.
        builder.HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.Property(e => e.PersonnelNumber).HasMaxLength(20).IsRequired();
        builder.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(e => e.MiddleName).HasMaxLength(100);
        builder.Property(e => e.Position).HasMaxLength(150).IsRequired();
        builder.Property(e => e.Email).HasMaxLength(256);
        builder.Property(e => e.Phone).HasMaxLength(30);

        // Computed in C# from other columns; there is no such column to map.
        builder.Ignore(e => e.FullName);
        builder.Ignore(e => e.ShortName);

        builder.HasIndex(e => e.PersonnelNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.LastName);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.HasIndex(c => c.Code).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.DrawingNumber).HasMaxLength(100);
        builder.Property(p => p.UnitOfMeasure).HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.Code).IsUnique().HasFilter("[IsDeleted] = 0");

        // Product search hits Name constantly; IsArchived is in nearly every filter.
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.IsArchived);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

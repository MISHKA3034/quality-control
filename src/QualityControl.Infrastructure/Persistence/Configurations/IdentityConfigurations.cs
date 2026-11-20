using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QualityControl.Domain.Entities;

namespace QualityControl.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.UserName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();

        // Filtered unique indexes: a soft-deleted account must not keep reserving
        // its login name, but two deleted rows may share one.
        builder.HasIndex(u => u.UserName)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // One account per employee.
        builder.HasOne(u => u.Employee)
            .WithOne(e => e.User)
            .HasForeignKey<User>(u => u.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);
        builder.Property(t => t.RevocationReason).HasMaxLength(256);

        // Every refresh is a lookup by hash; without this it is a table scan.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)

            // Tokens are worthless without their user; let them go with it.
            .OnDelete(DeleteBehavior.Cascade);

        // Must mirror the filter on User. Without it EF still returns the token but
        // leaves the required User navigation null for a soft-deleted account, and
        // the refresh path would dereference it. Filtering here makes such a token
        // simply not resolve, which is the behaviour we want anyway.
        builder.HasQueryFilter(t => !t.User.IsDeleted);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(t => t.RequestedByIp).HasMaxLength(64);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });

        builder.HasOne(t => t.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mirrors the filter on User, for the same reason as RefreshToken.
        builder.HasQueryFilter(t => !t.User.IsDeleted);
    }
}

using Lalana.Application.Auth;
using Lalana.Domain.Identity;
using Lalana.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lalana.Infrastructure.Persistence.Configurations;

internal sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<StoredCode>
{
    public void Configure(EntityTypeBuilder<StoredCode> code)
    {
        code.ToTable("verification_codes");
        code.HasKey(c => c.Id);
        code.Property(c => c.Purpose).HasConversion<string>().HasMaxLength(32);
        code.Property(c => c.Destination).HasMaxLength(254);
        code.Property(c => c.Hash).HasMaxLength(64);
        code.HasIndex(c => new { c.Destination, c.CreatedAt });
        code.HasOne<AppUser>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<StoredRefreshToken>
{
    public void Configure(EntityTypeBuilder<StoredRefreshToken> token)
    {
        token.ToTable("refresh_tokens");
        token.HasKey(t => t.Id);
        token.Property(t => t.Hash).HasMaxLength(64);
        token.HasIndex(t => t.Hash).IsUnique();
        token.HasIndex(t => t.FamilyId);
        token.HasIndex(t => t.UserId);
        token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>Stored form of a <see cref="Domain.Profiles.Profile"/>.</summary>
internal sealed class ProfileRow
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class ProfileConfiguration : IEntityTypeConfiguration<ProfileRow>
{
    public void Configure(EntityTypeBuilder<ProfileRow> profile)
    {
        profile.ToTable("profiles");
        profile.HasKey(p => p.Id);
        profile.Property(p => p.Id).ValueGeneratedNever();
        profile.Property(p => p.FirstName).HasMaxLength(PersonName.MaxLength);
        profile.Property(p => p.LastName).HasMaxLength(PersonName.MaxLength);
        profile.HasOne<AppUser>().WithOne().HasForeignKey<ProfileRow>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
    }
}

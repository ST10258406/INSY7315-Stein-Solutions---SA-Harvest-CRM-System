using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(x => x.Id);

        // SHA-256 hex (64 chars) — never the raw token. See RefreshToken's remarks.
        //
        // Deliberately kept on the original "Token" column (and its 512 length) rather than
        // a new column: rows written before hashing hold raw tokens, which can never match a
        // hash lookup, so they're invalidated with no data migration — and the generated
        // schema migration stays safe to apply to a table that already has rows.
        builder.Property(x => x.TokenHash)
            .HasColumnName("Token")
            .IsRequired()
            .HasMaxLength(512);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique();

        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        builder.HasIndex(x => x.FamilyId);

        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsRevoked).HasDefaultValue(false);

        builder.HasOne(x => x.User)
            .WithMany() // add a Tokens collection on User only if you actually need to navigate that way
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade); // if the user is gone, their sessions are meaningless
    }
}

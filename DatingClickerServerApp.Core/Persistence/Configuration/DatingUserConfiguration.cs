using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DatingClickerServerApp.Core.Persistence.Configuration
{
    public class DatingUserConfiguration : IEntityTypeConfiguration<DatingUser>
    {
        public void Configure(EntityTypeBuilder<DatingUser> builder)
        {
            builder.HasKey(du => du.Id);

            builder.Property(du => du.Id)
                   .IsRequired()
                   .HasDefaultValueSql("gen_random_uuid()"); // Добавлено для автогенерации Guid

            builder.Property(du => du.ExternalId)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.CreatedDate)
                .IsRequired();

            builder.Property(du => du.UpdatedDate)
                .IsRequired();

            builder.Property(du => du.Name)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(du => du.IsVerified)
                .IsRequired();

            builder.Property(du => du.Age)
                .IsRequired(false);

            builder.Property(du => du.HasChildren)
                .IsRequired(false);

            builder.Property(du => du.Height)
                .IsRequired(false);

            builder.Property(du => du.PreviewUrl)
                .IsRequired(true);

            builder.Property(du => du.About)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.Property(du => du.CityName)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(du => du.Interests)
                .IsRequired(false);

            builder.Property(e => e.JsonData)
                .HasColumnType("jsonb")
                .IsRequired(true);

            builder.HasMany(du => du.Actions)
               .WithOne(dua => dua.DatingUser)
               .HasForeignKey(dua => dua.DatingUserId);

            builder.HasIndex(du => du.ExternalId)
               .IsUnique();
        }
    }
}

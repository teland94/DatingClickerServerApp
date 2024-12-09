using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingClickerServerApp.Core.Persistence.Configuration
{
    public class DatingUserActionConfiguration : IEntityTypeConfiguration<DatingUserAction>
    {
        public void Configure(EntityTypeBuilder<DatingUserAction> builder)
        {
            builder.HasKey(dua => dua.Id);

            builder.Property(dua => dua.Id)
                   .IsRequired()
                   .HasDefaultValueSql("gen_random_uuid()"); // Добавлено для автогенерации Guid

            builder.Property(dua => dua.CreatedDate)
                .IsRequired();

            builder.Property(dua => dua.ActionType)
                .HasConversion<string>()
                .IsRequired();

            builder.Property(dua => dua.SuperLikeText)
                .IsRequired(false)
                .HasMaxLength(300);

            builder.HasOne(dua => dua.DatingUser)
                .WithMany(du => du.Actions)
                .HasForeignKey(dua => dua.DatingUserId);

            builder.HasOne(dua => dua.DatingAccount)
                .WithMany(du => du.Actions)
                .HasForeignKey(dua => dua.DatingAccountId);
        }
    }
}

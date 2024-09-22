using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingClickerServerApp.Common.Persistence
{
    public class DatingUserActionConfiguration : IEntityTypeConfiguration<DatingUserAction>
    {
        public void Configure(EntityTypeBuilder<DatingUserAction> builder)
        {
            builder.HasKey(dua => dua.Id);

            builder.Property(dua => dua.Id)
                   .IsRequired()
                   .HasDefaultValueSql("gen_random_uuid()"); // Добавлено для автогенерации Guid

            builder.Property(u => u.CreatedDate)
                .IsRequired();

            builder.Property(dua => dua.ActionType)
                .HasConversion<string>()
                .IsRequired();

            builder.HasOne(dua => dua.DatingUser)
                .WithMany(du => du.Actions)
                .HasForeignKey(dua => dua.DatingUserId);
        }
    }
}

using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace DatingClickerServerApp.Core.Persistence.Configuration
{
    public class BlacklistedDatingUserConfiguration : IEntityTypeConfiguration<BlacklistedDatingUser>
    {
        public void Configure(EntityTypeBuilder<BlacklistedDatingUser> builder)
        {
            builder.HasKey(bdu => bdu.Id);

            builder.HasOne(bdu => bdu.DatingUser)
                .WithOne(du => du.BlacklistedDatingUser)
                .HasForeignKey<BlacklistedDatingUser>(bdu => bdu.Id)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(bdu => bdu.CreatedDate)
                .IsRequired();
        }
    }
}

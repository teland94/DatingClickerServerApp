using DatingClickerServerApp.Common.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DatingClickerServerApp.Core.Persistence.Configuration
{
    public class DatingAccountConfiguration : IEntityTypeConfiguration<DatingAccount>
    {
        public void Configure(EntityTypeBuilder<DatingAccount> builder)
        {
            builder.HasKey(da => da.Id);

            builder.Property(da => da.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(da => da.AppUserId)
                .IsRequired();

            builder.Property(da => da.AppName)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(da => da.JsonAuthData)
                .IsRequired();

            builder.Property(da => da.JsonProfileData)
                .IsRequired();

            builder.Property(da => da.CreatedDate)
                .IsRequired();

            builder.Property(da => da.UpdatedDate)
                .IsRequired();
        }
    }
}
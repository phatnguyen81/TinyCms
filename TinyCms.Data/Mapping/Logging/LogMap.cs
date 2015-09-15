using TinyCms.Core.Domain.Logging;

namespace TinyCms.Data.Mapping.Logging
{
    public partial class LogMap : CmsEntityTypeConfiguration<Log>
    {
        public LogMap()
        {
            this.ToTable("Log");
            this.HasKey(l => l.Id);
            this.Property(l => l.ShortMessage).IsRequired();
            this.Property(l => l.IpAddress).HasMaxLength(200);

            this.Ignore(l => l.LogLevel);

            this.HasOptional(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
            .WillCascadeOnDelete(true);

        }
    }
}
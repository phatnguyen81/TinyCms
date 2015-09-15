using TinyCms.Core.Domain.Security;

namespace TinyCms.Data.Mapping.Security
{
    public partial class AclRecordMap : CmsEntityTypeConfiguration<AclRecord>
    {
        public AclRecordMap()
        {
            this.ToTable("AclRecord");
            this.HasKey(ar => ar.Id);

            this.Property(ar => ar.EntityName).IsRequired().HasMaxLength(400);

            this.HasRequired(ar => ar.UserRole)
                .WithMany()
                .HasForeignKey(ar => ar.UserRoleId)
                .WillCascadeOnDelete(true);
        }
    }
}
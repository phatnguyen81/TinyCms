using TinyCms.Core.Domain.Users;

namespace TinyCms.Data.Mapping.Users
{
    public partial class UserMap : CmsEntityTypeConfiguration<User>
    {
        public UserMap()
        {
            this.ToTable("User");
            this.HasKey(c => c.Id);
            this.Property(u => u.Username).HasMaxLength(1000);
            this.Property(u => u.Email).HasMaxLength(1000);
            this.Property(u => u.SystemName).HasMaxLength(400);

            this.Ignore(u => u.PasswordFormat);

            this.HasMany(c => c.UserRoles)
                .WithMany()
                .Map(m => m.ToTable("User_UserRole_Mapping"));

        }
    }
}
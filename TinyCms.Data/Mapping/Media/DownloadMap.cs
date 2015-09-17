using TinyCms.Core.Domain.Media;

namespace TinyCms.Data.Mapping.Media
{
    public partial class DownloadMap : CmsEntityTypeConfiguration<Download>
    {
        public DownloadMap()
        {
            this.ToTable("Download");
            this.HasKey(p => p.Id);
            this.Property(p => p.DownloadBinary).IsMaxLength();
        }
    }
}
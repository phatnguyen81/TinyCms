﻿using TinyCms.Core.Domain.Logging;

namespace TinyCms.Data.Mapping.Logging
{
    public partial class ActivityLogTypeMap : CmsEntityTypeConfiguration<ActivityLogType>
    {
        public ActivityLogTypeMap()
        {
            this.ToTable("ActivityLogType");
            this.HasKey(alt => alt.Id);

            this.Property(alt => alt.SystemKeyword).IsRequired().HasMaxLength(100);
            this.Property(alt => alt.Name).IsRequired().HasMaxLength(200);
        }
    }
}

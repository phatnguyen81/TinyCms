using System.Data.Entity.ModelConfiguration;

namespace TinyCms.Data.Mapping
{
    public abstract class CmsEntityTypeConfiguration<T> : EntityTypeConfiguration<T> where T : class
    {
        protected CmsEntityTypeConfiguration()
        {
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code to constructors
        /// </summary>
        protected virtual void PostInitialize()
        {
            
        }
    }
}
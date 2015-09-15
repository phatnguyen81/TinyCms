using System.Collections.Generic;
using TinyCms.Core.Domain.Security;

namespace TinyCms.Core.Domain.Users
{
    /// <summary>
    /// Represents a user role
    /// </summary>
    public partial class UserRole : BaseEntity
    {
        private ICollection<PermissionRecord> _permissionRecords;

        /// <summary>
        /// Gets or sets the user role name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user role is marked as free shiping
        /// </summary>
        public bool FreeShipping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user role is marked as tax exempt
        /// </summary>
        public bool TaxExempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user role is active
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user role is system
        /// </summary>
        public bool IsSystemRole { get; set; }

        /// <summary>
        /// Gets or sets the user role system name
        /// </summary>
        public string SystemName { get; set; }

        /// <summary>
        /// Gets or sets a product identifier that is required by this user role. 
        /// A user is added to this user role once a specified product is purchased.
        /// </summary>
        public int PurchasedWithProductId { get; set; }
        
        /// <summary>
        /// Gets or sets the permission records
        /// </summary>
        public virtual ICollection<PermissionRecord> PermissionRecords
        {
            get { return _permissionRecords ?? (_permissionRecords = new List<PermissionRecord>()); }
            protected set { _permissionRecords = value; }
        }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using TinyCms.Core;
using TinyCms.Core.Caching;
using TinyCms.Core.Data;
using TinyCms.Core.Domain.Users;
using TinyCms.Core.Domain.Security;
using TinyCms.Services.Users;
using TinyCms.Services.Localization;
using TinyCms.Services.Localization;

namespace TinyCms.Services.Security
{
    /// <summary>
    /// Permission service
    /// </summary>
    public partial class PermissionService : IPermissionService
    {
        #region Constants
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : user role ID
        /// {1} : permission system name
        /// </remarks>
        private const string PERMISSIONS_ALLOWED_KEY = "TinyCms.permission.allowed-{0}-{1}";
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string PERMISSIONS_PATTERN_KEY = "TinyCms.permission.";
        #endregion

        #region Fields

        private readonly IRepository<PermissionRecord> _permissionRecordRepository;
        private readonly IUserService _userService;
        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="permissionRecordRepository">Permission repository</param>
        /// <param name="userService">User service</param>
        /// <param name="workContext">Work context</param>
        /// <param name="localizationService">Localization service</param>
        /// <param name="languageService">Language service</param>
        /// <param name="cacheManager">Cache manager</param>
        public PermissionService(IRepository<PermissionRecord> permissionRecordRepository,
            IUserService userService,
            IWorkContext workContext,
             ILocalizationService localizationService,
            ILanguageService languageService,
            ICacheManager cacheManager)
        {
            this._permissionRecordRepository = permissionRecordRepository;
            this._userService = userService;
            this._workContext = workContext;
            this._localizationService = localizationService;
            this._languageService = languageService;
            this._cacheManager = cacheManager;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionRecordSystemName">Permission record system name</param>
        /// <param name="userRole">User role</param>
        /// <returns>true - authorized; otherwise, false</returns>
        protected virtual bool Authorize(string permissionRecordSystemName, UserRole userRole)
        {
            if (String.IsNullOrEmpty(permissionRecordSystemName))
                return false;
            
            string key = string.Format(PERMISSIONS_ALLOWED_KEY, userRole.Id, permissionRecordSystemName);
            return _cacheManager.Get(key, () =>
            {
                foreach (var permission1 in userRole.PermissionRecords)
                    if (permission1.SystemName.Equals(permissionRecordSystemName, StringComparison.InvariantCultureIgnoreCase))
                        return true;

                return false;
            });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual void DeletePermissionRecord(PermissionRecord permission)
        {
            if (permission == null)
                throw new ArgumentNullException("permission");

            _permissionRecordRepository.Delete(permission);

            _cacheManager.RemoveByPattern(PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="permissionId">Permission identifier</param>
        /// <returns>Permission</returns>
        public virtual PermissionRecord GetPermissionRecordById(int permissionId)
        {
            if (permissionId == 0)
                return null;

            return _permissionRecordRepository.GetById(permissionId);
        }

        /// <summary>
        /// Gets a permission
        /// </summary>
        /// <param name="systemName">Permission system name</param>
        /// <returns>Permission</returns>
        public virtual PermissionRecord GetPermissionRecordBySystemName(string systemName)
        {
            if (String.IsNullOrWhiteSpace(systemName))
                return null;

            var query = from pr in _permissionRecordRepository.Table
                        where  pr.SystemName == systemName
                        orderby pr.Id
                        select pr;

            var permissionRecord = query.FirstOrDefault();
            return permissionRecord;
        }

        /// <summary>
        /// Gets all permissions
        /// </summary>
        /// <returns>Permissions</returns>
        public virtual IList<PermissionRecord> GetAllPermissionRecords()
        {
            var query = from pr in _permissionRecordRepository.Table
                        orderby pr.Name
                        select pr;
            var permissions = query.ToList();
            return permissions;
        }

        /// <summary>
        /// Inserts a permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual void InsertPermissionRecord(PermissionRecord permission)
        {
            if (permission == null)
                throw new ArgumentNullException("permission");

            _permissionRecordRepository.Insert(permission);

            _cacheManager.RemoveByPattern(PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Updates the permission
        /// </summary>
        /// <param name="permission">Permission</param>
        public virtual void UpdatePermissionRecord(PermissionRecord permission)
        {
            if (permission == null)
                throw new ArgumentNullException("permission");

            _permissionRecordRepository.Update(permission);

            _cacheManager.RemoveByPattern(PERMISSIONS_PATTERN_KEY);
        }

        /// <summary>
        /// Install permissions
        /// </summary>
        /// <param name="permissionProvider">Permission provider</param>
        public virtual void InstallPermissions(IPermissionProvider permissionProvider)
        {
            //install new permissions
            var permissions = permissionProvider.GetPermissions();
            foreach (var permission in permissions)
            {
                var permission1 = GetPermissionRecordBySystemName(permission.SystemName);
                if (permission1 == null)
                {
                    //new permission (install it)
                    permission1 = new PermissionRecord
                    {
                        Name = permission.Name,
                        SystemName = permission.SystemName,
                        Category = permission.Category,
                    };


                    //default user role mappings
                    var defaultPermissions = permissionProvider.GetDefaultPermissions();
                    foreach (var defaultPermission in defaultPermissions)
                    {
                        var userRole = _userService.GetUserRoleBySystemName(defaultPermission.UserRoleSystemName);
                        if (userRole == null)
                        {
                            //new role (save it)
                            userRole = new UserRole
                            {
                                Name = defaultPermission.UserRoleSystemName,
                                Active = true,
                                SystemName = defaultPermission.UserRoleSystemName
                            };
                            _userService.InsertUserRole(userRole);
                        }


                        var defaultMappingProvided = (from p in defaultPermission.PermissionRecords
                                                      where p.SystemName == permission1.SystemName
                                                      select p).Any();
                        var mappingExists = (from p in userRole.PermissionRecords
                                             where p.SystemName == permission1.SystemName
                                             select p).Any();
                        if (defaultMappingProvided && !mappingExists)
                        {
                            permission1.UserRoles.Add(userRole);
                        }
                    }

                    //save new permission
                    InsertPermissionRecord(permission1);

                    //save localization
                    permission1.SaveLocalizedPermissionName(_localizationService, _languageService);
                }
            }
        }

        /// <summary>
        /// Uninstall permissions
        /// </summary>
        /// <param name="permissionProvider">Permission provider</param>
        public virtual void UninstallPermissions(IPermissionProvider permissionProvider)
        {
            var permissions = permissionProvider.GetPermissions();
            foreach (var permission in permissions)
            {
                var permission1 = GetPermissionRecordBySystemName(permission.SystemName);
                if (permission1 != null)
                {
                    DeletePermissionRecord(permission1);

                    //delete permission locales
                    permission1.DeleteLocalizedPermissionName(_localizationService, _languageService);
                }
            }

        }
        
        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission record</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize(PermissionRecord permission)
        {
            return Authorize(permission, _workContext.CurrentUser);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permission">Permission record</param>
        /// <param name="user">User</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize(PermissionRecord permission, User user)
        {
            if (permission == null)
                return false;

            if (user == null)
                return false;

            //old implementation of Authorize method
            //var userRoles = user.UserRoles.Where(cr => cr.Active);
            //foreach (var role in userRoles)
            //    foreach (var permission1 in role.PermissionRecords)
            //        if (permission1.SystemName.Equals(permission.SystemName, StringComparison.InvariantCultureIgnoreCase))
            //            return true;

            //return false;

            return Authorize(permission.SystemName, user);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionRecordSystemName">Permission record system name</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize(string permissionRecordSystemName)
        {
            return Authorize(permissionRecordSystemName, _workContext.CurrentUser);
        }

        /// <summary>
        /// Authorize permission
        /// </summary>
        /// <param name="permissionRecordSystemName">Permission record system name</param>
        /// <param name="user">User</param>
        /// <returns>true - authorized; otherwise, false</returns>
        public virtual bool Authorize(string permissionRecordSystemName, User user)
        {
            if (String.IsNullOrEmpty(permissionRecordSystemName))
                return false;

            var userRoles = user.UserRoles.Where(cr => cr.Active);
            foreach (var role in userRoles)
                if (Authorize(permissionRecordSystemName, role))
                    //yes, we have such permission
                    return true;
            
            //no permission found
            return false;
        }

        #endregion
    }
}
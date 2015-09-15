using System;
using System.Linq;
using TinyCms.Core;
using TinyCms.Core.Domain.Users;
using TinyCms.Services.Localization;
using TinyCms.Services.Security;

namespace TinyCms.Services.Users
{
    /// <summary>
    /// User registration service
    /// </summary>
    public partial class UserRegistrationService : IUserRegistrationService
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILocalizationService _localizationService;
        private readonly UserSettings _userSettings;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="userService">User service</param>
        /// <param name="encryptionService">Encryption service</param>
        /// <param name="localizationService">Localization service</param>
        /// <param name="userSettings">User settings</param>
        public UserRegistrationService(IUserService userService, 
            IEncryptionService encryptionService, 
            ILocalizationService localizationService,
            UserSettings userSettings)
        {
            this._userService = userService;
            this._encryptionService = encryptionService;
            this._localizationService = localizationService;
            this._userSettings = userSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validate user
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <param name="password">Password</param>
        /// <returns>Result</returns>
        public virtual UserLoginResults ValidateUser(string usernameOrEmail, string password)
        {
            User user;
            if (_userSettings.UsernamesEnabled)
                user = _userService.GetUserByUsername(usernameOrEmail);
            else
                user = _userService.GetUserByEmail(usernameOrEmail);

            if (user == null)
                return UserLoginResults.UserNotExist;
            if (user.Deleted)
                return UserLoginResults.Deleted;
            if (!user.Active)
                return UserLoginResults.NotActive;
            //only registered can login
            if (!user.IsRegistered())
                return UserLoginResults.NotRegistered;

            string pwd = "";
            switch (user.PasswordFormat)
            {
                case PasswordFormat.Encrypted:
                    pwd = _encryptionService.EncryptText(password);
                    break;
                case PasswordFormat.Hashed:
                    pwd = _encryptionService.CreatePasswordHash(password, user.PasswordSalt, _userSettings.HashedPasswordFormat);
                    break;
                default:
                    pwd = password;
                    break;
            }

            bool isValid = pwd == user.Password;
            if (!isValid)
                return UserLoginResults.WrongPassword;

            //save last login date
            user.LastLoginDateUtc = DateTime.UtcNow;
            _userService.UpdateUser(user);
            return UserLoginResults.Successful;
        }

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Result</returns>
        public virtual UserRegistrationResult RegisterUser(UserRegistrationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            if (request.User == null)
                throw new ArgumentException("Can't load current user");

            var result = new UserRegistrationResult();
            if (request.User.IsSearchEngineAccount())
            {
                result.AddError("Search engine can't be registered");
                return result;
            }
            if (request.User.IsBackgroundTaskAccount())
            {
                result.AddError("Background task account can't be registered");
                return result;
            }
            if (request.User.IsRegistered())
            {
                result.AddError("Current user is already registered");
                return result;
            }
            if (String.IsNullOrEmpty(request.Email))
            {
                result.AddError(_localizationService.GetResource("Account.Register.Errors.EmailIsNotProvided"));
                return result;
            }
            if (!CommonHelper.IsValidEmail(request.Email))
            {
                result.AddError(_localizationService.GetResource("Common.WrongEmail"));
                return result;
            }
            if (String.IsNullOrWhiteSpace(request.Password))
            {
                result.AddError(_localizationService.GetResource("Account.Register.Errors.PasswordIsNotProvided"));
                return result;
            }
            if (_userSettings.UsernamesEnabled)
            {
                if (String.IsNullOrEmpty(request.Username))
                {
                    result.AddError(_localizationService.GetResource("Account.Register.Errors.UsernameIsNotProvided"));
                    return result;
                }
            }

            //validate unique user
            if (_userService.GetUserByEmail(request.Email) != null)
            {
                result.AddError(_localizationService.GetResource("Account.Register.Errors.EmailAlreadyExists"));
                return result;
            }
            if (_userSettings.UsernamesEnabled)
            {
                if (_userService.GetUserByUsername(request.Username) != null)
                {
                    result.AddError(_localizationService.GetResource("Account.Register.Errors.UsernameAlreadyExists"));
                    return result;
                }
            }

            //at this point request is valid
            request.User.Username = request.Username;
            request.User.Email = request.Email;
            request.User.PasswordFormat = request.PasswordFormat;

            switch (request.PasswordFormat)
            {
                case PasswordFormat.Clear:
                    {
                        request.User.Password = request.Password;
                    }
                    break;
                case PasswordFormat.Encrypted:
                    {
                        request.User.Password = _encryptionService.EncryptText(request.Password);
                    }
                    break;
                case PasswordFormat.Hashed:
                    {
                        string saltKey = _encryptionService.CreateSaltKey(5);
                        request.User.PasswordSalt = saltKey;
                        request.User.Password = _encryptionService.CreatePasswordHash(request.Password, saltKey, _userSettings.HashedPasswordFormat);
                    }
                    break;
                default:
                    break;
            }

            request.User.Active = request.IsApproved;
            
            //add to 'Registered' role
            var registeredRole = _userService.GetUserRoleBySystemName(SystemUserRoleNames.Registered);
            if (registeredRole == null)
                throw new CmsException("'Registered' role could not be loaded");
            request.User.UserRoles.Add(registeredRole);
            //remove from 'Guests' role
            var guestRole = request.User.UserRoles.FirstOrDefault(cr => cr.SystemName == SystemUserRoleNames.Guests);
            if (guestRole != null)
                request.User.UserRoles.Remove(guestRole);
          
            _userService.UpdateUser(request.User);
            return result;
        }
        
        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Result</returns>
        public virtual ChangePasswordResult ChangePassword(ChangePasswordRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            var result = new ChangePasswordResult();
            if (String.IsNullOrWhiteSpace(request.Email))
            {
                result.AddError(_localizationService.GetResource("Account.ChangePassword.Errors.EmailIsNotProvided"));
                return result;
            }
            if (String.IsNullOrWhiteSpace(request.NewPassword))
            {
                result.AddError(_localizationService.GetResource("Account.ChangePassword.Errors.PasswordIsNotProvided"));
                return result;
            }

            var user = _userService.GetUserByEmail(request.Email);
            if (user == null)
            {
                result.AddError(_localizationService.GetResource("Account.ChangePassword.Errors.EmailNotFound"));
                return result;
            }


            var requestIsValid = false;
            if (request.ValidateRequest)
            {
                //password
                string oldPwd = "";
                switch (user.PasswordFormat)
                {
                    case PasswordFormat.Encrypted:
                        oldPwd = _encryptionService.EncryptText(request.OldPassword);
                        break;
                    case PasswordFormat.Hashed:
                        oldPwd = _encryptionService.CreatePasswordHash(request.OldPassword, user.PasswordSalt, _userSettings.HashedPasswordFormat);
                        break;
                    default:
                        oldPwd = request.OldPassword;
                        break;
                }

                bool oldPasswordIsValid = oldPwd == user.Password;
                if (!oldPasswordIsValid)
                    result.AddError(_localizationService.GetResource("Account.ChangePassword.Errors.OldPasswordDoesntMatch"));

                if (oldPasswordIsValid)
                    requestIsValid = true;
            }
            else
                requestIsValid = true;


            //at this point request is valid
            if (requestIsValid)
            {
                switch (request.NewPasswordFormat)
                {
                    case PasswordFormat.Clear:
                        {
                            user.Password = request.NewPassword;
                        }
                        break;
                    case PasswordFormat.Encrypted:
                        {
                            user.Password = _encryptionService.EncryptText(request.NewPassword);
                        }
                        break;
                    case PasswordFormat.Hashed:
                        {
                            string saltKey = _encryptionService.CreateSaltKey(5);
                            user.PasswordSalt = saltKey;
                            user.Password = _encryptionService.CreatePasswordHash(request.NewPassword, saltKey, _userSettings.HashedPasswordFormat);
                        }
                        break;
                    default:
                        break;
                }
                user.PasswordFormat = request.NewPasswordFormat;
                _userService.UpdateUser(user);
            }

            return result;
        }

        /// <summary>
        /// Sets a user email
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="newEmail">New email</param>
        public virtual void SetEmail(User user, string newEmail)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            if (newEmail == null)
                throw new CmsException("Email cannot be null");

            newEmail = newEmail.Trim();
            string oldEmail = user.Email;

            if (!CommonHelper.IsValidEmail(newEmail))
                throw new CmsException(_localizationService.GetResource("Account.EmailUsernameErrors.NewEmailIsNotValid"));

            if (newEmail.Length > 100)
                throw new CmsException(_localizationService.GetResource("Account.EmailUsernameErrors.EmailTooLong"));

            var user2 = _userService.GetUserByEmail(newEmail);
            if (user2 != null && user.Id != user2.Id)
                throw new CmsException(_localizationService.GetResource("Account.EmailUsernameErrors.EmailAlreadyExists"));

            user.Email = newEmail;
            _userService.UpdateUser(user);

            //update newsletter subscription (if required)
            //if (!String.IsNullOrEmpty(oldEmail) && !oldEmail.Equals(newEmail, StringComparison.InvariantCultureIgnoreCase))
            //{
            //    foreach (var store in _storeService.GetAllStores())
            //    {
            //        var subscriptionOld = _newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreId(oldEmail, store.Id);
            //        if (subscriptionOld != null)
            //        {
            //            subscriptionOld.Email = newEmail;
            //            _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscriptionOld);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Sets a user username
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="newUsername">New Username</param>
        public virtual void SetUsername(User user, string newUsername)
        {
            if (user == null)
                throw new ArgumentNullException("user");

            if (!_userSettings.UsernamesEnabled)
                throw new CmsException("Usernames are disabled");

            if (!_userSettings.AllowUsersToChangeUsernames)
                throw new CmsException("Changing usernames is not allowed");

            newUsername = newUsername.Trim();

            if (newUsername.Length > 100)
                throw new CmsException(_localizationService.GetResource("Account.EmailUsernameErrors.UsernameTooLong"));

            var user2 = _userService.GetUserByUsername(newUsername);
            if (user2 != null && user.Id != user2.Id)
                throw new CmsException(_localizationService.GetResource("Account.EmailUsernameErrors.UsernameAlreadyExists"));

            user.Username = newUsername;
            _userService.UpdateUser(user);
        }

        #endregion
    }
}
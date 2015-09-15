using System;
using System.Web;
using System.Web.Security;
using TinyCms.Core.Domain.Users;
using TinyCms.Services.Users;

namespace TinyCms.Services.Authentication
{
    /// <summary>
    /// Authentication service
    /// </summary>
    public partial class FormsAuthenticationService : IAuthenticationService
    {
        private readonly HttpContextBase _httpContext;
        private readonly IUserService _userService;
        private readonly UserSettings _userSettings;
        private readonly TimeSpan _expirationTimeSpan;

        private User _cachedUser;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="userService">User service</param>
        /// <param name="userSettings">User settings</param>
        public FormsAuthenticationService(HttpContextBase httpContext,
            IUserService userService, UserSettings userSettings)
        {
            this._httpContext = httpContext;
            this._userService = userService;
            this._userSettings = userSettings;
            this._expirationTimeSpan = FormsAuthentication.Timeout;
        }


        public virtual void SignIn(User user, bool createPersistentCookie)
        {
            var now = DateTime.UtcNow.ToLocalTime();

            var ticket = new FormsAuthenticationTicket(
                1 /*version*/,
                _userSettings.UsernamesEnabled ? user.Username : user.Email,
                now,
                now.Add(_expirationTimeSpan),
                createPersistentCookie,
                _userSettings.UsernamesEnabled ? user.Username : user.Email,
                FormsAuthentication.FormsCookiePath);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket);
            cookie.HttpOnly = true;
            if (ticket.IsPersistent)
            {
                cookie.Expires = ticket.Expiration;
            }
            cookie.Secure = FormsAuthentication.RequireSSL;
            cookie.Path = FormsAuthentication.FormsCookiePath;
            if (FormsAuthentication.CookieDomain != null)
            {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            _httpContext.Response.Cookies.Add(cookie);
            _cachedUser = user;
        }

        public virtual void SignOut()
        {
            _cachedUser = null;
            FormsAuthentication.SignOut();
        }

        public virtual User GetAuthenticatedUser()
        {
            if (_cachedUser != null)
                return _cachedUser;

            if (_httpContext == null ||
                _httpContext.Request == null ||
                !_httpContext.Request.IsAuthenticated ||
                !(_httpContext.User.Identity is FormsIdentity))
            {
                return null;
            }

            var formsIdentity = (FormsIdentity)_httpContext.User.Identity;
            var user = GetAuthenticatedUserFromTicket(formsIdentity.Ticket);
            if (user != null && user.Active && !user.Deleted && user.IsRegistered())
                _cachedUser = user;
            return _cachedUser;
        }

        public virtual User GetAuthenticatedUserFromTicket(FormsAuthenticationTicket ticket)
        {
            if (ticket == null)
                throw new ArgumentNullException("ticket");

            var usernameOrEmail = ticket.UserData;

            if (String.IsNullOrWhiteSpace(usernameOrEmail))
                return null;
            var user = _userSettings.UsernamesEnabled
                ? _userService.GetUserByUsername(usernameOrEmail)
                : _userService.GetUserByEmail(usernameOrEmail);
            return user;
        }
    }
}
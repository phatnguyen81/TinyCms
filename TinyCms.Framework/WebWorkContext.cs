using System;
using System.Linq;
using System.Web;
using Nop.Services.Helpers;
using TinyCms.Core;
using TinyCms.Core.Domain.Localization;
using TinyCms.Core.Domain.Users;
using TinyCms.Core.Fakes;
using TinyCms.Services.Authentication;
using TinyCms.Services.Common;
using TinyCms.Services.Helpers;
using TinyCms.Services.Localization;
using TinyCms.Services.Users;
using TinyCms.Web.Framework.Localization;

namespace TinyCms.Framework
{
    /// <summary>
    /// Work context for web application
    /// </summary>
    public partial class WebWorkContext : IWorkContext
    {
        #region Const

        private const string UserCookieName = "TinyCms.User";

        #endregion

        #region Fields

        private readonly HttpContextBase _httpContext;
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILanguageService _languageService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly LocalizationSettings _localizationSettings;
        private readonly IUserAgentHelper _userAgentHelper;

        private User _cachedUser;
        private User _originalUserIfImpersonated;
        private Language _cachedLanguage;

        #endregion

        #region Ctor

        public WebWorkContext(HttpContextBase httpContext,
            IUserService userService,
            IAuthenticationService authenticationService,
            ILanguageService languageService,
            IGenericAttributeService genericAttributeService,
            LocalizationSettings localizationSettings, 
            IUserAgentHelper userAgentHelper)
        {
            this._httpContext = httpContext;
            this._userService = userService;
            this._authenticationService = authenticationService;
            this._languageService = languageService;
            this._genericAttributeService = genericAttributeService;
            this._localizationSettings = localizationSettings;
            this._userAgentHelper = userAgentHelper;
        }

        #endregion

        #region Utilities

        protected virtual HttpCookie GetUserCookie()
        {
            if (_httpContext == null || _httpContext.Request == null)
                return null;

            return _httpContext.Request.Cookies[UserCookieName];
        }

        protected virtual void SetUserCookie(Guid UserGuid)
        {
            if (_httpContext != null && _httpContext.Response != null)
            {
                var cookie = new HttpCookie(UserCookieName);
                cookie.HttpOnly = true;
                cookie.Value = UserGuid.ToString();
                if (UserGuid == Guid.Empty)
                {
                    cookie.Expires = DateTime.Now.AddMonths(-1);
                }
                else
                {
                    int cookieExpires = 24*365; //TODO make configurable
                    cookie.Expires = DateTime.Now.AddHours(cookieExpires);
                }

                _httpContext.Response.Cookies.Remove(UserCookieName);
                _httpContext.Response.Cookies.Add(cookie);
            }
        }

        protected virtual Language GetLanguageFromUrl()
        {
            if (_httpContext == null || _httpContext.Request == null)
                return null;

            string virtualPath = _httpContext.Request.AppRelativeCurrentExecutionFilePath;
            string applicationPath = _httpContext.Request.ApplicationPath;
            if (!virtualPath.IsLocalizedUrl(applicationPath, false))
                return null;

            var seoCode = virtualPath.GetLanguageSeoCodeFromUrl(applicationPath, false);
            if (String.IsNullOrEmpty(seoCode))
                return null;

            var language = _languageService
                .GetAllLanguages()
                .FirstOrDefault(l => seoCode.Equals(l.UniqueSeoCode, StringComparison.InvariantCultureIgnoreCase));
            if (language != null && language.Published )
            {
                return language;
            }

            return null;
        }

        protected virtual Language GetLanguageFromBrowserSettings()
        {
            if (_httpContext == null ||
                _httpContext.Request == null ||
                _httpContext.Request.UserLanguages == null)
                return null;

            var userLanguage = _httpContext.Request.UserLanguages.FirstOrDefault();
            if (String.IsNullOrEmpty(userLanguage))
                return null;

            var language = _languageService
                .GetAllLanguages()
                .FirstOrDefault(l => userLanguage.Equals(l.LanguageCulture, StringComparison.InvariantCultureIgnoreCase));
            if (language != null && language.Published)
            {
                return language;
            }

            return null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current User
        /// </summary>
        public virtual User CurrentUser
        {
            get
            {
                if (_cachedUser != null)
                    return _cachedUser;

                User User = null;
                if (_httpContext == null || _httpContext is FakeHttpContext)
                {
                    //check whether request is made by a background task
                    //in this case return built-in User record for background task
                    User = _userService.GetUserBySystemName(SystemUserNames.BackgroundTask);
                }

                //check whether request is made by a search engine
                //in this case return built-in User record for search engines 
                //or comment the following two lines of code in order to disable this functionality
                if (User == null || User.Deleted || !User.Active)
                {
                    if (_userAgentHelper.IsSearchEngine())
                        User = _userService.GetUserBySystemName(SystemUserNames.SearchEngine);
                }

                //registered user
                if (User == null || User.Deleted || !User.Active)
                {
                    User = _authenticationService.GetAuthenticatedUser();
                }

                //impersonate user if required (currently used for 'phone order' support)
                if (User != null && !User.Deleted && User.Active)
                {
                    var impersonatedUserId = User.GetAttribute<int?>(SystemUserAttributeNames.ImpersonatedUserId);
                    if (impersonatedUserId.HasValue && impersonatedUserId.Value > 0)
                    {
                        var impersonatedUser = _userService.GetUserById(impersonatedUserId.Value);
                        if (impersonatedUser != null && !impersonatedUser.Deleted && impersonatedUser.Active)
                        {
                            //set impersonated User
                            _originalUserIfImpersonated = User;
                            User = impersonatedUser;
                        }
                    }
                }

                //load guest User
                if (User == null || User.Deleted || !User.Active)
                {
                    var UserCookie = GetUserCookie();
                    if (UserCookie != null && !String.IsNullOrEmpty(UserCookie.Value))
                    {
                        Guid UserGuid;
                        if (Guid.TryParse(UserCookie.Value, out UserGuid))
                        {
                            var UserByCookie = _userService.GetUserByGuid(UserGuid);
                            if (UserByCookie != null &&
                                //this User (from cookie) should not be registered
                                !UserByCookie.IsRegistered())
                                User = UserByCookie;
                        }
                    }
                }

                //create guest if not exists
                if (User == null || User.Deleted || !User.Active)
                {
                    User = _userService.InsertGuestUser();
                }


                //validation
                if (!User.Deleted && User.Active)
                {
                    SetUserCookie(User.UserGuid);
                    _cachedUser = User;
                }

                return _cachedUser;
            }
            set
            {
                SetUserCookie(value.UserGuid);
                _cachedUser = value;
            }
        }

        /// <summary>
        /// Gets or sets the original User (in case the current one is impersonated)
        /// </summary>
        public virtual User OriginalUserIfImpersonated
        {
            get
            {
                return _originalUserIfImpersonated;
            }
        }

    
        /// <summary>
        /// Get or set current user working language
        /// </summary>
        public virtual Language WorkingLanguage
        {
            get
            {
                if (_cachedLanguage != null)
                    return _cachedLanguage;
                
                Language detectedLanguage = null;
                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    //get language from URL
                    detectedLanguage = GetLanguageFromUrl();
                }
                if (detectedLanguage == null && _localizationSettings.AutomaticallyDetectLanguage)
                {
                    //get language from browser settings
                    //but we do it only once
                    if (!this.CurrentUser.GetAttribute<bool>(SystemUserAttributeNames.LanguageAutomaticallyDetected, 
                        _genericAttributeService))
                    {
                        detectedLanguage = GetLanguageFromBrowserSettings();
                        if (detectedLanguage != null)
                        {
                            _genericAttributeService.SaveAttribute(this.CurrentUser, SystemUserAttributeNames.LanguageAutomaticallyDetected,
                                 true);
                        }
                    }
                }
                if (detectedLanguage != null)
                {
                    //the language is detected. now we need to save it
                    if (this.CurrentUser.GetAttribute<int>(SystemUserAttributeNames.LanguageId,
                        _genericAttributeService) != detectedLanguage.Id)
                    {
                        _genericAttributeService.SaveAttribute(this.CurrentUser, SystemUserAttributeNames.LanguageId,
                            detectedLanguage.Id);
                    }
                }

                var allLanguages = _languageService.GetAllLanguages();
                //find current User language
                var languageId = this.CurrentUser.GetAttribute<int>(SystemUserAttributeNames.LanguageId,
                    _genericAttributeService);
                var language = allLanguages.FirstOrDefault(x => x.Id == languageId);
                if (language == null)
                {
                    //it not specified, then return the first (filtered by current store) found one
                    language = allLanguages.FirstOrDefault();
                }
                if (language == null)
                {
                    //it not specified, then return the first found one
                    language = _languageService.GetAllLanguages().FirstOrDefault();
                }

                //cache
                _cachedLanguage = language;
                return _cachedLanguage;
            }
            set
            {
                var languageId = value != null ? value.Id : 0;
                _genericAttributeService.SaveAttribute(this.CurrentUser,
                    SystemUserAttributeNames.LanguageId,
                    languageId);

                //reset cache
                _cachedLanguage = null;
            }
        }

      

        /// <summary>
        /// Get or set value indicating whether we're in admin area
        /// </summary>
        public virtual bool IsAdmin { get; set; }

        #endregion
    }
}

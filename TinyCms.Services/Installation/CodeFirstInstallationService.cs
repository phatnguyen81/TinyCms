using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyCms.Core;
using TinyCms.Core.Data;
using TinyCms.Core.Domain;
using TinyCms.Core.Domain.Common;
using TinyCms.Core.Domain.Localization;
using TinyCms.Core.Domain.Logging;
using TinyCms.Core.Domain.Media;
using TinyCms.Core.Domain.Messages;
using TinyCms.Core.Domain.Security;
using TinyCms.Core.Domain.Seo;
using TinyCms.Core.Domain.Tasks;
using TinyCms.Core.Domain.Users;
using TinyCms.Core.Infrastructure;
using TinyCms.Services.Common;
using TinyCms.Services.Configuration;
using TinyCms.Services.Helpers;
using TinyCms.Services.Localization;
using TinyCms.Services.Users;

namespace TinyCms.Services.Installation
{
    public partial class CodeFirstInstallationService : IInstallationService
    {
        #region Fields

        private readonly IRepository<Language> _languageRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IRepository<UrlRecord> _urlRecordRepository;
        private readonly IRepository<EmailAccount> _emailAccountRepository;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly IRepository<ActivityLogType> _activityLogTypeRepository;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public CodeFirstInstallationService(
            IRepository<Language> languageRepository,
            IRepository<User> userRepository,
            IRepository<UserRole> userRoleRepository,
            IRepository<UrlRecord> urlRecordRepository,
            IRepository<EmailAccount> emailAccountRepository,
            IRepository<ScheduleTask> scheduleTaskRepository,
            IRepository<ActivityLogType> activityLogTypeRepository,
            IGenericAttributeService genericAttributeService,
            IWebHelper webHelper)
        {
            this._languageRepository = languageRepository;
            this._userRepository = userRepository;
            this._userRoleRepository = userRoleRepository;
            this._urlRecordRepository = urlRecordRepository;
            this._emailAccountRepository = emailAccountRepository;
            this._scheduleTaskRepository = scheduleTaskRepository;
            this._genericAttributeService = genericAttributeService;
            this._webHelper = webHelper;
            _activityLogTypeRepository = activityLogTypeRepository;
        }

        #endregion

        #region Utilities

        protected virtual void InstallLanguages()
        {
            var language = new Language
            {
                Name = "English",
                LanguageCulture = "en-US",
                UniqueSeoCode = "en",
                FlagImageFileName = "us.png",
                Published = true,
                DisplayOrder = 1
            };
            _languageRepository.Insert(language);
        }

        protected virtual void InstallLocaleResources()
        {
            //'English' language
            var language = _languageRepository.Table.Single(l => l.Name == "English");

            //save resources
            foreach (var filePath in System.IO.Directory.EnumerateFiles(_webHelper.MapPath("~/App_Data/Localization/"), "*.nopres.xml", SearchOption.TopDirectoryOnly))
            {
                var localesXml = File.ReadAllText(filePath);
                var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
                localizationService.ImportResourcesFromXml(language, localesXml);
            }

        }

      
        protected virtual void InstallUsersAndUsers(string defaultUserEmail, string defaultUserPassword)
        {
            var crAdministrators = new UserRole
            {
                Name = "Administrators",
                Active = true,
                IsSystemRole = true,
                SystemName = SystemUserRoleNames.Administrators,
            };
          
            var crRegistered = new UserRole
            {
                Name = "Registered",
                Active = true,
                IsSystemRole = true,
                SystemName = SystemUserRoleNames.Registered,
            };
            var crGuests = new UserRole
            {
                Name = "Guests",
                Active = true,
                IsSystemRole = true,
                SystemName = SystemUserRoleNames.Guests,
            };
          
            var userRoles = new List<UserRole>
                                {
                                    crAdministrators,
                                    crRegistered,
                                    crGuests,
                                };
            _userRoleRepository.Insert(userRoles);

            //admin user
            var adminUser = new User
            {
                UserGuid = Guid.NewGuid(),
                Email = defaultUserEmail,
                Username = defaultUserEmail,
                Password = defaultUserPassword,
                PasswordFormat = PasswordFormat.Clear,
                PasswordSalt = "",
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
          
            adminUser.UserRoles.Add(crAdministrators);
            adminUser.UserRoles.Add(crRegistered);
            _userRepository.Insert(adminUser);
            //set default user name
            _genericAttributeService.SaveAttribute(adminUser, SystemUserAttributeNames.FirstName, "Phat");
            _genericAttributeService.SaveAttribute(adminUser, SystemUserAttributeNames.LastName, "Nguyen");


            //search engine (crawler) built-in user
            var searchEngineUser = new User
            {
                Email = "builtin@search_engine_record.com",
                UserGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system guest record used for requests from search engines.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemUserNames.SearchEngine,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
            searchEngineUser.UserRoles.Add(crGuests);
            _userRepository.Insert(searchEngineUser);


            //built-in user for background tasks
            var backgroundTaskUser = new User
            {
                Email = "builtin@background-task-record.com",
                UserGuid = Guid.NewGuid(),
                PasswordFormat = PasswordFormat.Clear,
                AdminComment = "Built-in system record used for background tasks.",
                Active = true,
                IsSystemAccount = true,
                SystemName = SystemUserNames.BackgroundTask,
                CreatedOnUtc = DateTime.UtcNow,
                LastActivityDateUtc = DateTime.UtcNow,
            };
            backgroundTaskUser.UserRoles.Add(crGuests);
            _userRepository.Insert(backgroundTaskUser);
        }

        protected virtual void HashDefaultUserPassword(string defaultUserEmail, string defaultUserPassword)
        {
            var userRegistrationService = EngineContext.Current.Resolve<IUserRegistrationService>();
            userRegistrationService.ChangePassword(new ChangePasswordRequest(defaultUserEmail, false,
                 PasswordFormat.Hashed, defaultUserPassword));
        }

        protected virtual void InstallEmailAccounts()
        {
            var emailAccounts = new List<EmailAccount>
                               {
                                   new EmailAccount
                                       {
                                           Email = "test@mail.com",
                                           DisplayName = "Site name",
                                           Host = "smtp.mail.com",
                                           Port = 25,
                                           Username = "123",
                                           Password = "123",
                                           EnableSsl = false,
                                           UseDefaultCredentials = false
                                       },
                               };
            _emailAccountRepository.Insert(emailAccounts);
        }

      
        protected virtual void InstallSettings()
        {
            var settingService = EngineContext.Current.Resolve<ISettingService>();
      

            settingService.SaveSetting(new CommonSettings
                {
                    UseSystemEmailForContactUsForm = true,
                    UseStoredProceduresIfSupported = true,
                    SitemapEnabled = true,
                    SitemapIncludeCategories = true,
                    SitemapIncludeManufacturers = true,
                    SitemapIncludeProducts = false,
                    DisplayJavaScriptDisabledWarning = false,
                    UseFullTextSearch = false,
                    FullTextMode = FulltextSearchMode.ExactMatch,
                    Log404Errors = true,
                    BreadcrumbDelimiter = "/",
                    RenderXuaCompatible = false,
                    XuaCompatibleValue = "IE=edge"
                });

            settingService.SaveSetting(new SeoSettings
                {
                    PageTitleSeparator = ". ",
                    PageTitleSeoAdjustment = PageTitleSeoAdjustment.PagenameAfterSitename,
                    DefaultTitle = "Your store",
                    DefaultMetaKeywords = "",
                    DefaultMetaDescription = "",
                    GenerateProductMetaDescription = true,
                    ConvertNonWesternChars = false,
                    AllowUnicodeCharsInUrls = true,
                    CanonicalUrlsEnabled = false,
                    WwwRequirement = WwwRequirement.NoMatter,
                    //we disable bundling out of the box because it requires a lot of server resources
                    EnableJsBundling = false,
                    EnableCssBundling = false,
                    TwitterMetaTags = true,
                    OpenGraphMetaTags = true,
                    ReservedUrlRecordSlugs = new List<string>
                    {
                        "admin", 
                        "install", 
                        "recentlyviewedproducts", 
                        "newproducts",
                        "compareproducts", 
                        "clearcomparelist",
                        "setproductreviewhelpfulness",
                        "login", 
                        "register", 
                        "logout", 
                        "cart",
                        "wishlist", 
                        "emailwishlist", 
                        "checkout", 
                        "onepagecheckout", 
                        "contactus", 
                        "passwordrecovery", 
                        "subscribenewsletter",
                        "blog", 
                        "boards", 
                        "inboxupdate",
                        "sentupdate", 
                        "news", 
                        "sitemap", 
                        "search",
                        "config", 
                        "eucookielawaccept", 
                        "page-not-found"
                    },
                });

            settingService.SaveSetting(new AdminAreaSettings
                {
                    DefaultGridPageSize = 15,
                    GridPageSizes = "10, 15, 20, 50, 100",
                    RichEditorAdditionalSettings = null,
                    RichEditorAllowJavaScript = false
                });

           

            settingService.SaveSetting(new LocalizationSettings
                {
                    DefaultAdminLanguageId = _languageRepository.Table.Single(l => l.Name == "English").Id,
                    UseImagesForLanguageSelection = false,
                    SeoFriendlyUrlsForLanguagesEnabled = false,
                    AutomaticallyDetectLanguage = false,
                    LoadAllLocaleRecordsOnStartup = true,
                    LoadAllLocalizedPropertiesOnStartup = true,
                    LoadAllUrlRecordsOnStartup = false,
                    IgnoreRtlPropertyForAdminArea = false,
                });

            settingService.SaveSetting(new UserSettings
                {
                    UsernamesEnabled = false,
                    CheckUsernameAvailabilityEnabled = false,
                    AllowUsersToChangeUsernames = false,
                    DefaultPasswordFormat = PasswordFormat.Hashed,
                    HashedPasswordFormat = "SHA1",
                    PasswordMinLength = 6,
                    PasswordRecoveryLinkDaysValid = 7,
                    UserRegistrationType = UserRegistrationType.Standard,
                    AllowUsersToUploadAvatars = false,
                    AvatarMaximumSizeBytes = 20000,
                    DefaultAvatarEnabled = true,
                    ShowUsersLocation = false,
                    ShowUsersJoinDate = false,
                    AllowViewingProfiles = false,
                    NotifyNewUserRegistration = false,
                    HideDownloadableProductsTab = false,
                    HideBackInStockSubscriptionsTab = false,
                    DownloadableProductsValidateUser = false,
                    UserNameFormat = UserNameFormat.ShowFirstName,
                    GenderEnabled = true,
                    DateOfBirthEnabled = true,
                    CompanyEnabled = true,
                    StreetAddressEnabled = false,
                    StreetAddress2Enabled = false,
                    ZipPostalCodeEnabled = false,
                    CityEnabled = false,
                    CountryEnabled = false,
                    CountryRequired = false,
                    StateProvinceEnabled = false,
                    StateProvinceRequired = false,
                    PhoneEnabled = false,
                    FaxEnabled = false,
                    AcceptPrivacyPolicyEnabled = false,
                    NewsletterEnabled = true,
                    NewsletterTickedByDefault = true,
                    HideNewsletterBlock = false,
                    NewsletterBlockAllowToUnsubscribe = false,
                    OnlineUserMinutes = 20,
                    SiteLastVisitedPage = false,
                    SuffixDeletedUsers = false,
                });



            settingService.SaveSetting(new MediaSettings
                {
                    AvatarPictureSize = 120,
                    ProductThumbPictureSize = 415,
                    ProductDetailsPictureSize = 550,
                    ProductThumbPictureSizeOnProductDetailsPage = 100,
                    AssociatedProductPictureSize = 220,
                    CategoryThumbPictureSize = 450,
                    ManufacturerThumbPictureSize = 420,
                    CartThumbPictureSize = 80,
                    MiniCartThumbPictureSize = 70,
                    AutoCompleteSearchThumbPictureSize = 20,
                    MaximumImageSize = 1980,
                    DefaultPictureZoomEnabled = false,
                    DefaultImageQuality = 80,
                    MultipleThumbDirectories = false
                });

            settingService.SaveSetting(new SiteInformationSettings
                {
                    SiteClosed = false,
                    SiteClosedAllowForAdmins = false,
                    AllowUserToSelectTheme = true,
                    DisplayMiniProfilerInPublicSite = false,
                    DisplayEuCookieLawWarning = false,
                 
                });

        
            settingService.SaveSetting(new SecuritySettings
                {
                    ForceSslForAllPages = false,
                    EncryptionKey = CommonHelper.GenerateRandomDigitCode(16),
                    AdminAreaAllowedIpAddresses = null,
                    EnableXsrfProtectionForAdminArea = true,
                    EnableXsrfProtectionForPublicSite = true,
                    HoneypotEnabled = false,
                    HoneypotInputName = "hpinput"
                });

           
            settingService.SaveSetting(new DateTimeSettings
                {
                    DefaultSiteTimeZoneId = "",
                    AllowUsersToSetTimeZone = false
                });

          

            var eaGeneral = _emailAccountRepository.Table.FirstOrDefault();
            if (eaGeneral == null)
                throw new Exception("Default email account cannot be loaded");
            settingService.SaveSetting(new EmailAccountSettings
                {
                    DefaultEmailAccountId = eaGeneral.Id
                });

          
        }
        
        protected virtual void InstallActivityLogTypes()
        {
            var activityLogTypes = new List<ActivityLogType>
                                      {
                                          //admin area activities
                                      
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewUser",
                                                  Enabled = true,
                                                  Name = "Add a new user"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewUserRole",
                                                  Enabled = true,
                                                  Name = "Add a new user role"
                                              },
                                        
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewSetting",
                                                  Enabled = true,
                                                  Name = "Add a new setting"
                                              },
                                        
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteUser",
                                                  Enabled = true,
                                                  Name = "Delete a user"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteUserRole",
                                                  Enabled = true,
                                                  Name = "Delete a user role"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteDiscount",
                                                  Enabled = true,
                                                  Name = "Delete a discount"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteGiftCard",
                                                  Enabled = true,
                                                  Name = "Delete a gift card"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteManufacturer",
                                                  Enabled = true,
                                                  Name = "Delete a manufacturer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteProduct",
                                                  Enabled = true,
                                                  Name = "Delete a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteProductAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a product attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteReturnRequest",
                                                  Enabled = true,
                                                  Name = "Delete a return request"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteSetting",
                                                  Enabled = true,
                                                  Name = "Delete a setting"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteSpecAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a specification attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteWidget",
                                                  Enabled = true,
                                                  Name = "Delete a widget"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCategory",
                                                  Enabled = true,
                                                  Name = "Edit category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCheckoutAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a checkout attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditUser",
                                                  Enabled = true,
                                                  Name = "Edit a user"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditUserRole",
                                                  Enabled = true,
                                                  Name = "Edit a user role"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditDiscount",
                                                  Enabled = true,
                                                  Name = "Edit a discount"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditGiftCard",
                                                  Enabled = true,
                                                  Name = "Edit a gift card"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditManufacturer",
                                                  Enabled = true,
                                                  Name = "Edit a manufacturer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditProduct",
                                                  Enabled = true,
                                                  Name = "Edit a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditProductAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a product attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditPromotionProviders",
                                                  Enabled = true,
                                                  Name = "Edit promotion providers"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditReturnRequest",
                                                  Enabled = true,
                                                  Name = "Edit a return request"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditSettings",
                                                  Enabled = true,
                                                  Name = "Edit setting(s)"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditSpecAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a specification attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditWidget",
                                                  Enabled = true,
                                                  Name = "Edit a widget"
                                              },
                                              //public store activities
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.ViewCategory",
                                                  Enabled = false,
                                                  Name = "Public store. View a category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.ViewManufacturer",
                                                  Enabled = false,
                                                  Name = "Public store. View a manufacturer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.ViewProduct",
                                                  Enabled = false,
                                                  Name = "Public store. View a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.PlaceOrder",
                                                  Enabled = false,
                                                  Name = "Public store. Place an order"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.SendPM",
                                                  Enabled = false,
                                                  Name = "Public store. Send PM"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.ContactUs",
                                                  Enabled = false,
                                                  Name = "Public store. Use contact us form"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddToCompareList",
                                                  Enabled = false,
                                                  Name = "Public store. Add to compare list"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddToShoppingCart",
                                                  Enabled = false,
                                                  Name = "Public store. Add to shopping cart"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddToWishlist",
                                                  Enabled = false,
                                                  Name = "Public store. Add to wishlist"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.Login",
                                                  Enabled = false,
                                                  Name = "Public store. Login"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.Logout",
                                                  Enabled = false,
                                                  Name = "Public store. Logout"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddProductReview",
                                                  Enabled = false,
                                                  Name = "Public store. Add product review"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddNewsComment",
                                                  Enabled = false,
                                                  Name = "Public store. Add news comment"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddBlogComment",
                                                  Enabled = false,
                                                  Name = "Public store. Add blog comment"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddForumTopic",
                                                  Enabled = false,
                                                  Name = "Public store. Add forum topic"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.EditForumTopic",
                                                  Enabled = false,
                                                  Name = "Public store. Edit forum topic"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.DeleteForumTopic",
                                                  Enabled = false,
                                                  Name = "Public store. Delete forum topic"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.AddForumPost",
                                                  Enabled = false,
                                                  Name = "Public store. Add forum post"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.EditForumPost",
                                                  Enabled = false,
                                                  Name = "Public store. Edit forum post"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicSite.DeleteForumPost",
                                                  Enabled = false,
                                                  Name = "Public store. Delete forum post"
                                              },
                                      };
            _activityLogTypeRepository.Insert(activityLogTypes);
        }


        protected virtual void InstallScheduleTasks()
        {
            var tasks = new List<ScheduleTask>
            {
                new ScheduleTask
                {
                    Name = "Send emails",
                    Seconds = 60,
                    Type = "Nop.Services.Messages.QueuedMessagesSendTask, Nop.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new ScheduleTask
                {
                    Name = "Keep alive",
                    Seconds = 300,
                    Type = "Nop.Services.Common.KeepAliveTask, Nop.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new ScheduleTask
                {
                    Name = "Delete guests",
                    Seconds = 600,
                    Type = "Nop.Services.Users.DeleteGuestsTask, Nop.Services",
                    Enabled = true,
                    StopOnError = false,
                },
                new ScheduleTask
                {
                    Name = "Clear cache",
                    Seconds = 600,
                    Type = "Nop.Services.Caching.ClearCacheTask, Nop.Services",
                    Enabled = false,
                    StopOnError = false,
                },
                new ScheduleTask
                {
                    Name = "Clear log",
                    //60 minutes
                    Seconds = 3600,
                    Type = "Nop.Services.Logging.ClearLogTask, Nop.Services",
                    Enabled = false,
                    StopOnError = false,
                },
            
            };

            _scheduleTaskRepository.Insert(tasks);
        }

    
        #endregion

        #region Methods

        public virtual void InstallData(string defaultUserEmail,
            string defaultUserPassword, bool installSampleData = true)
        {
            InstallLanguages();
            InstallUsersAndUsers(defaultUserEmail, defaultUserPassword);
            InstallEmailAccounts();
            InstallSettings();
            InstallLocaleResources();
            InstallActivityLogTypes();
            HashDefaultUserPassword(defaultUserEmail, defaultUserPassword);
            InstallScheduleTasks();

        }

        #endregion
    }
}
using TinyCms.Core.Configuration;

namespace TinyCms.Core.Domain
{
    public class SiteInformationSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a store name
        /// </summary>
        public string SiteName { get; set; }

        /// <summary>
        /// Gets or sets a store URL
        /// </summary>
        public string SiteUrl { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether store is closed
        /// </summary>
        public bool SiteClosed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether administrators can visit a closed store
        /// </summary>
        public bool SiteClosedAllowForAdmins { get; set; }

        /// <summary>
        /// Gets or sets a default store theme for desktops
        /// </summary>
        public string DefaultSiteThemeForDesktops { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether users are allowed to select a theme
        /// </summary>
        public bool AllowUserToSelectTheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mobile devices supported
        /// </summary>
        public bool MobileDevicesSupported { get; set; }

        /// <summary>
        /// Gets or sets a default store theme used by mobile devices (if enabled)
        /// </summary>
        public string DefaultSiteThemeForMobileDevices { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all requests will be handled as mobile devices (used for testing)
        /// </summary>
        public bool EmulateMobileDevice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether mini profiler should be displayed in public store (used for debugging)
        /// </summary>
        public bool DisplayMiniProfilerInPublicSite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should display warnings about the new EU cookie law
        /// </summary>
        public bool DisplayEuCookieLawWarning { get; set; }
    }
}

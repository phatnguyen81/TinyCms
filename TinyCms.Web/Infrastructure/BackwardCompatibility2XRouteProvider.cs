﻿using System;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Routing;
using TinyCms.Web.Framework.Localization;
using TinyCms.Web.Framework.Mvc.Routes;

namespace TinyCms.Web.Infrastructure
{
    //Routes used for backward compatibility with 2.x versions of nopCommerce
    public partial class BackwardCompatibility2XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            var supportPreviousNopcommerceVersions =
                !String.IsNullOrEmpty(ConfigurationManager.AppSettings["SupportPreviousNopcommerceVersions"]) &&
                Convert.ToBoolean(ConfigurationManager.AppSettings["SupportPreviousNopcommerceVersions"]);
            if (!supportPreviousNopcommerceVersions)
                return;

            //products
            routes.MapLocalizedRoute("", "p/{productId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectProductById", SeName = UrlParameter.Optional },
                new { productId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });

            //categories
            routes.MapLocalizedRoute("", "c/{categoryId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectCategoryById", SeName = UrlParameter.Optional },
                new { categoryId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });

            //manufacturers
            routes.MapLocalizedRoute("", "m/{manufacturerId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectManufacturerById", SeName = UrlParameter.Optional },
                new { manufacturerId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });

            //news
            routes.MapLocalizedRoute("", "news/{newsItemId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectNewsItemById", SeName = UrlParameter.Optional },
                new { newsItemId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });

            //blog
            routes.MapLocalizedRoute("", "blog/{blogPostId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectBlogPostById", SeName = UrlParameter.Optional },
                new { blogPostId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });

            //topic
            routes.MapLocalizedRoute("", "t/{SystemName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectTopicBySystemName" },
                new[] { "TinyCms.Web.Controllers" });

            //vendors
            routes.MapLocalizedRoute("", "vendor/{vendorId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectVendorById", SeName = UrlParameter.Optional },
                new { vendorId = @"\d+" },
                new[] { "TinyCms.Web.Controllers" });
        }

        public int Priority
        {
            get
            {
                //register it after all other IRouteProvider are processed
                return -1000;
            }
        }
    }
}

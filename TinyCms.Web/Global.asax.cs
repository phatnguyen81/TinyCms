﻿using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using FluentValidation.Mvc;
using StackExchange.Profiling;
using StackExchange.Profiling.Mvc;
using TinyCms.Core;
using TinyCms.Core.Data;
using TinyCms.Core.Domain;
using TinyCms.Core.Domain.Common;
using TinyCms.Core.Infrastructure;
using TinyCms.Framework;
using TinyCms.Framework.Mvc;
using TinyCms.Framework.ViewEngines.Razor;
using TinyCms.Services.Logging;
using TinyCms.Services.Tasks;
using TinyCms.Web.Controllers;
using TinyCms.Web.Framework.Mvc.Routes;

namespace TinyCms.Web
{
    public class MvcApplication : HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("favicon.ico");
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            //register custom routes (plugins, etc)
            var routePublisher = EngineContext.Current.Resolve<IRoutePublisher>();
            routePublisher.RegisterRoutes(routes);

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "TinyCms.Web.Controllers" }
            );
        }

        protected void Application_Start()
        {
            //initialize engine context
            EngineContext.Initialize(false);

            bool databaseInstalled = DataSettingsHelper.DatabaseIsInstalled();

            if (databaseInstalled)
            {
                //remove all view engines
                ViewEngines.Engines.Clear();
                //except the themeable razor view engine we use
                ViewEngines.Engines.Add(new CustomRazorViewEngine());
            }


            //Add some functionality on top of the default ModelMetadataProvider
            ModelMetadataProviders.Current = new CmsMetadataProvider();

            //Registering some regular mvc stuff
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);

            //fluent validation
            DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false;
            ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new CmsValidatorFactory()));

            //start scheduled tasks
            if (databaseInstalled)
            {
                TaskManager.Instance.Initialize();
                TaskManager.Instance.Start();
            }

            //miniprofiler
            if (databaseInstalled)
            {
                if (EngineContext.Current.Resolve<SiteInformationSettings>().DisplayMiniProfilerInPublicSite)
                {
                    GlobalFilters.Filters.Add(new ProfilingActionFilter());
                }
            }

            //log application start
            if (databaseInstalled)
            {
                try
                {
                    //log
                    var logger = EngineContext.Current.Resolve<ILogger>();
                    logger.Information("Application started", null, null);
                }
                catch (Exception)
                {
                    //don't throw new exception if occurs
                }
            }
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            //ignore static resources
            var webHelper = EngineContext.Current.Resolve<IWebHelper>();
            if (webHelper.IsStaticResource(this.Request))
                return;

            //keep alive page requested (we ignore it to prevent creating a guest customer records)
            string keepAliveUrl = string.Format("{0}keepalive/index", webHelper.GetStoreLocation());
            if (webHelper.GetThisPageUrl(false).StartsWith(keepAliveUrl, StringComparison.InvariantCultureIgnoreCase))
                return;

            //ensure database is installed
            if (!DataSettingsHelper.DatabaseIsInstalled())
            {
                string installUrl = string.Format("{0}install", webHelper.GetStoreLocation());
                if (!webHelper.GetThisPageUrl(false).StartsWith(installUrl, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Response.Redirect(installUrl);
                }
            }

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //miniprofiler
            if (EngineContext.Current.Resolve<SiteInformationSettings>().DisplayMiniProfilerInPublicSite)
            {
                MiniProfiler.Start();
                //store a value indicating whether profiler was started
                HttpContext.Current.Items["cms.MiniProfilerStarted"] = true;
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            //miniprofiler
            var miniProfilerStarted = HttpContext.Current.Items.Contains("cms.MiniProfilerStarted") &&
                 Convert.ToBoolean(HttpContext.Current.Items["cms.MiniProfilerStarted"]);
            if (miniProfilerStarted)
            {
                MiniProfiler.Stop();
            }
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            //we don't do it in Application_BeginRequest because a user is not authenticated yet
            SetWorkingCulture();
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            var exception = Server.GetLastError();

            //log error
            LogException(exception);

            //process 404 HTTP errors
            var httpException = exception as HttpException;
            if (httpException != null && httpException.GetHttpCode() == 404)
            {
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                if (!webHelper.IsStaticResource(this.Request))
                {
                    Response.Clear();
                    Server.ClearError();
                    Response.TrySkipIisCustomErrors = true;

                    // Call target Controller and pass the routeData.
                    IController errorController = EngineContext.Current.Resolve<CommonController>();

                    var routeData = new RouteData();
                    routeData.Values.Add("controller", "Common");
                    routeData.Values.Add("action", "PageNotFound");

                    errorController.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
                }
            }
        }

        protected void SetWorkingCulture()
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //ignore static resources
            var webHelper = EngineContext.Current.Resolve<IWebHelper>();
            if (webHelper.IsStaticResource(this.Request))
                return;

            //keep alive page requested (we ignore it to prevent creation of guest customer records)
            string keepAliveUrl = string.Format("{0}keepalive/index", webHelper.GetStoreLocation());
            if (webHelper.GetThisPageUrl(false).StartsWith(keepAliveUrl, StringComparison.InvariantCultureIgnoreCase))
                return;


            if (webHelper.GetThisPageUrl(false).StartsWith(string.Format("{0}admin", webHelper.GetStoreLocation()),
                StringComparison.InvariantCultureIgnoreCase))
            {
                //admin area


                //always set culture to 'en-US'
                //we set culture of admin area to 'en-US' because current implementation of Telerik grid 
                //doesn't work well in other cultures
                //e.g., editing decimal value in russian culture
                CommonHelper.SetTelerikCulture();
            }
            else
            {
                //public store
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var culture = new CultureInfo(workContext.WorkingLanguage.LanguageCulture);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
        }

        protected void LogException(Exception exc)
        {
            if (exc == null)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            //ignore 404 HTTP errors
            var httpException = exc as HttpException;
            if (httpException != null && httpException.GetHttpCode() == 404 &&
                !EngineContext.Current.Resolve<CommonSettings>().Log404Errors)
                return;

            try
            {
                //log
                var logger = EngineContext.Current.Resolve<ILogger>();
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                logger.Error(exc.Message, exc, workContext.CurrentUser);
            }
            catch (Exception)
            {
                //don't throw new exception if occurs
            }
        }
    }
}
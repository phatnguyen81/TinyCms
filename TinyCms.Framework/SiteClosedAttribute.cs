using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TinyCms.Core;
using TinyCms.Core.Data;
using TinyCms.Core.Domain;
using TinyCms.Core.Domain.Users;
using TinyCms.Core.Infrastructure;

namespace TinyCms.Framework
{
    public class SiteClosedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            HttpRequestBase request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            string actionName = filterContext.ActionDescriptor.ActionName;
            if (String.IsNullOrEmpty(actionName))
                return;

            string controllerName = filterContext.Controller.ToString();
            if (String.IsNullOrEmpty(controllerName))
                return;

            //don't apply filter to child methods
            if (filterContext.IsChildAction)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var siteInformationSettings = EngineContext.Current.Resolve<SiteInformationSettings>();
            if (!siteInformationSettings.SiteClosed)
                return;

            //<controller, action>
            var allowedPages = new List<Tuple<string, string>>();
            //login page
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.UserController", "Login"));
            //logout page
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.UserController", "Logout"));
            //site closed page
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.CommonController", "EuCookieLawAccept"));
            //the method (AJAX) for accepting EU Cookie law
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.CommonController", "StoreClosed"));
            //the change language page (request)
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.CommonController", "SetLanguage"));
            //contact us page
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.CommonController", "ContactUs"));
            allowedPages.Add(new Tuple<string, string>("TinyCms.Web.Controllers.CommonController", "ContactUsSend"));
            var isPageAllowed = allowedPages.Any(
                x => controllerName.Equals(x.Item1, StringComparison.InvariantCultureIgnoreCase) &&
                     actionName.Equals(x.Item2, StringComparison.InvariantCultureIgnoreCase));
            if (isPageAllowed)
                return;

            //topics accessible when a site is closed
            //if (controllerName.Equals("TinyCms.Web.Controllers.TopicController", StringComparison.InvariantCultureIgnoreCase) &&
            //    actionName.Equals("TopicDetails", StringComparison.InvariantCultureIgnoreCase))
            //{
            //    var topicService = EngineContext.Current.Resolve<ITopicService>();
            //    var siteContext = EngineContext.Current.Resolve<IStoreContext>();
            //    var allowedTopicIds = topicService.GetAllTopics(siteContext.CurrentStore.Id)
            //        .Where(t => t.AccessibleWhenStoreClosed)
            //        .Select(t => t.Id)
            //        .ToList();
            //    var requestedTopicId = filterContext.RouteData.Values["topicId"] as int?;
            //    if (requestedTopicId.HasValue && allowedTopicIds.Contains(requestedTopicId.Value))
            //        return;
            //}

            //allow admin access
            if (siteInformationSettings.SiteClosedAllowForAdmins &&
                EngineContext.Current.Resolve<IWorkContext>().CurrentUser.IsAdmin())
                return;


            var siteClosedUrl = new UrlHelper(filterContext.RequestContext).RouteUrl("SiteClosed");
            filterContext.Result = new RedirectResult(siteClosedUrl);
        }
    }
}

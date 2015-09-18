using System.Web.Mvc;

namespace TinyCms.Framework.Mvc
{
    public class CmsModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var model = base.BindModel(controllerContext, bindingContext);
            if (model is BaseCmsModel)
            {
                ((BaseCmsModel)model).BindModel(controllerContext, bindingContext);
            }
            return model;
        }
    }
}

using TinyCms.Framework.Mvc;

namespace TinyCms.Web.Framework.Mvc
{
    public class DeleteConfirmationModel : BaseCmsEntityModel
    {
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string WindowId { get; set; }
    }
}
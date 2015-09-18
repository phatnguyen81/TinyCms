using TinyCms.Framework.Mvc;
using TinyCms.Web.Framework.Mvc;

namespace TinyCms.Web.Models.Common
{
    public partial class LanguageModel : BaseCmsEntityModel
    {
        public string Name { get; set; }

        public string FlagImageFileName { get; set; }

    }
}
using System.Collections.Generic;
using TinyCms.Framework.Mvc;
using TinyCms.Web.Framework.Mvc;

namespace TinyCms.Web.Models.Common
{
    public partial class LanguageSelectorModel : BaseCmsModel
    {
        public LanguageSelectorModel()
        {
            AvailableLanguages = new List<LanguageModel>();
        }

        public IList<LanguageModel> AvailableLanguages { get; set; }

        public int CurrentLanguageId { get; set; }

        public bool UseImages { get; set; }
    }
}
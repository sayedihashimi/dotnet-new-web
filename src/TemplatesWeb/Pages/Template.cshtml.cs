using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplatesShared;
using Microsoft.Extensions.Options;

namespace TemplatesWeb.Pages
{
    public class TemplateModel : BasePageModel
    {
        public string TemplateId { get; set; }
        public Template Template { get; set; }
        public TemplatePack TemplatePack { get; set; }

        public TemplateModel(IOptions<TemplateWebConfig> config) : base(config) {

        }

        public string PackId { get; set; }

        public void OnGet(string packId, string templateId)
        {
            PackId = packId;
            TemplateId = templateId;

            TemplatePack = GetFromApi<TemplatePack>($"templatepack/{packId}");
            Template = new TemplateSearcher().GetTemplateById(TemplateId, TemplatePack);
            // new TemplateSearcher().FindTemplatePackById(templateName, new l)
            // get the template pack
            // http://localhost:61747/api/templatepack/Microsoft.AspNetCore.SpaTemplates

        }
    }
}
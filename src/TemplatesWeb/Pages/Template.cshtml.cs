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

        public async Task OnGetAsync(string packId, string templateId)
        {
            PackId = packId;
            TemplateId = templateId;

            TemplatePack = await GetFromApiAsync<TemplatePack>($"templatepack/{packId}");
            Template = new TemplateSearcher().GetTemplateById(TemplateId, TemplatePack);
        }
    }
}
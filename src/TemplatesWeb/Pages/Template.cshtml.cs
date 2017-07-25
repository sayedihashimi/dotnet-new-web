using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplatesShared;

namespace TemplatesWeb.Pages
{
    public class TemplateModel : PageModel
    {
        public string TemplateName { get; set; }
        public Template _template { get; set; }
        public TemplatePack _templatePack { get; set; }

        public string PackId { get; set; }

        public void OnGet(string packId, string templateName)
        {
            this.PackId = packId;
            this.TemplateName = templateName;

            // get the template pack
            // http://localhost:61747/api/templatepack/Microsoft.AspNetCore.SpaTemplates

        }
    }
}
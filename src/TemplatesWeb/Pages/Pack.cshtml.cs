using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplatesShared;
using Microsoft.Extensions.Options;

namespace TemplatesWeb.Pages {
    public class PackModel : BasePageModel {
        public string PackId { get; set; }
        public TemplatePack TemplatePack { get; set; }
        public PackModel(IOptions<TemplateWebConfig> config) : base(config) {

        }
        
        public void OnGet(string packId) {
            if (string.IsNullOrWhiteSpace(packId)) {
                return;
            }

            PackId = packId;
            TemplatePack = GetFromApi<TemplatePack>($"templatepack/{PackId}");
        }
    }
}

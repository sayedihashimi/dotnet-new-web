using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using TemplatesShared;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace TemplatesWeb.Pages {
    public class IndexModel : BasePageModel {
        public List<TemplatePack> TemplatePacks { get; set; }

        [BindProperty]
        public string SearchText { get; set; }

        public IndexModel(IOptions<TemplateWebConfig> config):base(config) {
        }
        public void OnGet() {
            TemplatePacks = GetFromApi<List<TemplatePack>>("templatepack");
        }

        public IActionResult OnPostAsync() {
            if (!ModelState.IsValid) {
                return Page();
            }

            return Redirect($"/Search/{SearchText}");
        }
    }
}

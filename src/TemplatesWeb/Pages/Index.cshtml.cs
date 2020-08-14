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
        public int OverallDownloads { get; set; }
        public int NumTemplates { get; set; }
        public int NumTemplatePacks { get; set; }
        public int NumAuthors { get; set; }
        [BindProperty(SupportsGet =true)]
        public int Skip { get; set; }
        [BindProperty(SupportsGet =true)]
        public int Take { get; set; }

        public IndexModel(IOptions<TemplateWebConfig> config):base(config) {
        }
        public async Task OnGetAsync() {
            // TemplatePacks = await GetFromApiAsync<List<TemplatePack>>("templatepack");

            if(Skip < 0) {
                Skip = 0;
            }
            if(Take < 0) {
                Take = 50;
            }

            var stats = await GetFromApiAsync<TemplateStats>("templatepack/stats");
            if(stats != null) {
                OverallDownloads = stats.NumDownloads;
                NumTemplates = stats.NumTemplates;
                NumTemplatePacks = stats.NumTemplatePacks;
                NumAuthors = stats.NumAuthors;
            }
            TemplatePacks = await GetFromApiAsync<List<TemplatePack>>($"templatepack/{Skip}/{Take}");

            var limitNumTempaltePacks = System.Environment.GetEnvironmentVariable("LimitNumOfTempaltePacks");
            if(!string.IsNullOrWhiteSpace(limitNumTempaltePacks) &&
                string.Compare(limitNumTempaltePacks, "True", StringComparison.OrdinalIgnoreCase) == 0) {
                TemplatePacks = TemplatePacks.GetRange(0, 10);
            }
        }

        public IActionResult OnPostAsync() {
            if (!ModelState.IsValid) {
                return Page();
            }

            return Redirect($"/Search/{SearchText}");
        }
    }
}

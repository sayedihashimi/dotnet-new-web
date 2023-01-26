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
    [IgnoreAntiforgeryToken]
    public class IndexModel : BasePageModel {
        public List<TemplatePack> TemplatePacks { get; set; }

        [BindProperty]
        public string SearchText { get; set; }
        public TemplateStats Stats { get; set; }
        [BindProperty(SupportsGet =true)]
        public int Skip { get; set; }
        [BindProperty(SupportsGet =true)]
        public int Take { get; set; }
        public ConditionalLink PreviousLink { get; set; }
        public ConditionalLink NextLink { get; set; }

        public IndexModel(IOptions<TemplateWebConfig> config):base(config) {
        }
        public async Task OnGetAsync() {
            // TemplatePacks = await GetFromApiAsync<List<TemplatePack>>("templatepack");

            Console.WriteLine($"tunnel URL: {Environment.GetEnvironmentVariable("VS_TUNNEL_URL")}");
            Console.WriteLine($"API tunnel URL: {Environment.GetEnvironmentVariable("VS_TUNNEL_URL_TemplatesApi")}");

            if (Skip < 0) {
                Skip = 0;
            }
            if(Take <= 0) {
                Take = 20;
            }

            Stats = await GetFromApiAsync<TemplateStats>("templatepack/stats");
            // to avoid nullref errors
            if (Stats == null) { Stats = new TemplateStats(); }

            //if(Skip < Stats.NumTemplatePacks - Take) {
            //    Skip = Stats.NumTemplatePacks - Take;
            //}

            TemplatePacks = await GetFromApiAsync<List<TemplatePack>>($"templatepack/{Skip}/{Take}");

            var limitNumTempaltePacks = System.Environment.GetEnvironmentVariable("LimitNumOfTempaltePacks");
            if(!string.IsNullOrWhiteSpace(limitNumTempaltePacks) &&
                string.Compare(limitNumTempaltePacks, "True", StringComparison.OrdinalIgnoreCase) == 0) {
                TemplatePacks = TemplatePacks.GetRange(0, 10);
            }

            string previousLink = "";
            string nextLink = "";
            int prevSkip = Skip - Take;
            int nextSkip = Skip + Take;
            
            if(prevSkip >= 0) {
                previousLink = $"?skip={prevSkip}";
            }
            if (nextSkip < Stats.NumTemplatePacks) {
                nextLink = $"?skip={nextSkip}";
            }

            PreviousLink = new ConditionalLink(prevSkip >= 0, previousLink);
            NextLink = new ConditionalLink(nextSkip < Stats.NumTemplatePacks, nextLink);
        }

        public IActionResult OnPostAsync() {
            if (!ModelState.IsValid) {
                return Page();
            }

            return Redirect($"/Search/{SearchText}");
        }

        public string GetIconUrlFor(TemplatePack templatePack) =>
            string.IsNullOrEmpty(templatePack?.IconUrl) ? Strings.DefaultTemplatePackIconUrl : templatePack?.IconUrl;
    }
}

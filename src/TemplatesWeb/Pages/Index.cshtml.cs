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

namespace TemplatesWeb.Pages
{
    public class IndexModel : PageModel
    {
        private TemplateWebConfig _config;
        public IndexModel(IOptions<TemplateWebConfig> config) {
            _config = config.Value;
        }
        public void OnGet()
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText(@"C:\data\mycode\dotnet-new-web\src\template-report.json");
                var res = JsonConvert.DeserializeObject<List<TemplatePack>>(jsonString);
                ViewData["url"] = _config.TemplatesApiBaseUrl;
             }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

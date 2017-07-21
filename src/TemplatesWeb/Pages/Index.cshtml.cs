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

namespace TemplatesWeb.Pages
{
    public class IndexModel : PageModel
    {
        private TemplateWebConfig _config;
        public IList<Template> _templates;
      
        public IndexModel(IOptions<TemplateWebConfig> config) {
            _config = config.Value;
            _templates = new List<Template>();
        }
        public async void OnGet()
        {
            try
            {
                string jsonString = System.IO.File.ReadAllText(@"C:\data\mycode\dotnet-new-web\src\template-report.json");
                var res = JsonConvert.DeserializeObject<List<TemplatePack>>(jsonString);
                
                var url = new Uri(new Uri(_config.TemplatesApiBaseUrl), "search").AbsoluteUri;
                ViewData["url"] = url;
                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(url).Result;
                string json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<Template>>(json);
                _templates = result;
                // List<Template>result =
                // client.GetAsync("/api/customerservice").Result;
                // string stringData = response.Content.ReadAsStringAsync().Result;
                // List<Customer> data = JsonConvert.DeserializeObject<List<Customer>>(stringData);

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

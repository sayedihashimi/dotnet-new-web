using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TemplatesShared;

namespace TemplatesWeb.Pages
{
    public abstract class BasePageModel : PageModel
    {
        public TemplateWebConfig Config { get; set; }
        public string BaseUrl { get; set; }
        public BasePageModel(IOptions<TemplateWebConfig> config) {
            Config = config.Value;

            BaseUrl = Config.TemplatesApiBaseUrl;
            if (string.IsNullOrWhiteSpace(BaseUrl) || BaseUrl.StartsWith(@"D:\", StringComparison.OrdinalIgnoreCase)) {
                BaseUrl = @"https://dotnetnew-api.azurewebsites.net/api/";
            }
        }
        
        public async Task<T> GetFromApiAsync<T>(string relurl) {
            var url = new Uri(new Uri(BaseUrl), relurl).AbsoluteUri;
            var client = new HttpClient();
            var response = await client.GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TemplatesShared;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Newtonsoft.Json;

namespace TemplatesWeb.Pages
{
    public class SearchModel : PageModel
    {
        private TemplateWebConfig _config { get; set; }
        public IList<Template> _templates { get; set; }
        private string _baseUrl { get; set; }
        [BindProperty]
        public string SearchText { get; set; }

        public List<Template> SearchResults { get; set; }
        public SearchModel(IOptions<TemplateWebConfig> config) {
            _config = config.Value;
            _templates = new List<Template>();

            _baseUrl = _config.TemplatesApiBaseUrl;
            if (string.IsNullOrWhiteSpace(_baseUrl) || _baseUrl.StartsWith(@"D:\")) {
                _baseUrl = @"http://dotnetnew-api.azurewebsites.net/api/";
            }
        }


        public void OnGet(string searchText)
        {
            this.SearchText = searchText;

            var url = new Uri(new Uri(_baseUrl), $"search/{searchText}").AbsoluteUri;
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;
            string json = response.Content.ReadAsStringAsync().Result;
            SearchResults = JsonConvert.DeserializeObject<List<Template>>(json);
        }
    }
}
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
    public class SearchModel : BasePageModel {
        [BindProperty]
        public string SearchText { get; set; }

        public List<Template> SearchResults { get; set; }
        public SearchModel(IOptions<TemplateWebConfig> config): base(config) {
        }

        public IActionResult OnGet(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText)){
                return RedirectToPage("/Index");
            }
            SearchText = searchText;
            SearchResults = GetFromApi<List<Template>>($"search/{searchText}");
            return Page();
        }
    }
}
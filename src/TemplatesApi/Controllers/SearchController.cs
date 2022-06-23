using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TemplatesShared;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TemplatesApi.Controllers
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        public SearchController(IWebHostEnvironment hostingEnvironment) {
            _hostingEnvironment = hostingEnvironment;
            _webRoot = _hostingEnvironment.WebRootPath;
            SetFilepath(@"template-report.json");
        }
        public static void SetFilepath(string filename) {
            string filepath = Path.Combine(_webRoot, filename);
            Filepath = filepath;
            TemplatePacks = TemplatePack.CreateFromFile(filepath);
        }
        private readonly IWebHostEnvironment _hostingEnvironment;
        private static string _webRoot { get; set; }
        private static string Filepath { get; set; }
        private static List<TemplatePack> TemplatePacks { get; set; }

        /// <summary>
        /// Returns all the templates
        /// </summary>
        /// <returns>All templates</returns>
        [HttpGet()]
        public List<Template> Get() {
            return Get(string.Empty);
        }
        /// <summary>
        /// Returns all templates that match the search term
        /// </summary>
        /// <param name="searchTerm">string that will be used to search for templates</param>
        /// <returns>Templates that include the search term</returns>
        [HttpGet("{searchTerm}")]
        public List<Template> Get(string searchTerm) {
            return new TemplateSearcher().Search(searchTerm, TemplatePacks);
        }
    }
}

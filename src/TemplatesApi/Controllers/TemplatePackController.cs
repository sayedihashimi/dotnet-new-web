using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TemplatesShared;
using System.Reflection;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace TemplatesApi.Controllers {
    [Route("api/[controller]")]
    public class TemplatePackController : Controller {
        private readonly IHostingEnvironment _hostingEnvironment;
        private static string _webRoot { get; set; }
        private static string Filepath { get; set; }
        private static List<TemplatePack> TemplatePacks { get; set; }

        public TemplatePackController(IHostingEnvironment hostingEnvironment) {
            _hostingEnvironment = hostingEnvironment;
            _webRoot = _hostingEnvironment.WebRootPath;
            SetFilepath(@"template-report.json");
        }
        public static void SetFilepath(string filename) {
            string filepath = Path.Combine(_webRoot, filename);
            Filepath = filepath;
            TemplatePacks = TemplatePack.CreateFromFile(filepath);
        }

        [HttpGet("{packageId}")]
        public TemplatePack Get(string packageId) {
            return new TemplateSearcher().FindTemplatePackById(packageId, TemplatePacks);
        }
    }
}

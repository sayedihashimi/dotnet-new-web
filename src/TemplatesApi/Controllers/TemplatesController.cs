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
    public class TemplatesController : Controller
    {
        public TemplatesController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            _webRoot = _hostingEnvironment.WebRootPath;
            SetFilepath(@"template-report.json");
        }
        public void SetFilepath(string filename)
        {
            string filepath = Path.Combine(_webRoot, filename);
            Filepath = filepath;
            TemplatePacks = TemplatePack.CreateFromFile(filepath);
        }
        private readonly IWebHostEnvironment _hostingEnvironment;
        private string _webRoot { get; set; }
        private string Filepath { get; set; }
        private List<TemplatePack> TemplatePacks { get; set; }

        // GET api/templates
        [HttpGet]
        public IEnumerable<TemplatePack> Get()
        {
            return TemplatePacks;
        }

        // GET api/templates/ID
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            foreach(var tp in TemplatePacks)
            {
                if (id.Equals(tp.Package, StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(tp);
                }
            }

            return NotFound();
        }
    }
}

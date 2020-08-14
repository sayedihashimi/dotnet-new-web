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
        private readonly IWebHostEnvironment _hostingEnvironment;
        private static string _webRoot { get; set; }
        private static string Filepath { get; set; }
        private static List<TemplatePack> TemplatePacks { get; set; }

        public TemplatePackController(IWebHostEnvironment hostingEnvironment) {
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
            return new TemplateSearcher().FindTemplatePackByName(packageId, TemplatePacks);
        }
        public List<TemplatePack> Get() {
            return TemplatePacks;
        }
        /// <summary>
        /// Will return a subset of the Template packs.
        /// Typically the number of elements returned is determined by take.
        /// 
        /// If skip is >= the num of packs, null will be returned
        /// </summary>
        /// <param name="skip">num of elements to skip</param>
        /// <param name="take">num of elements that should be returned</param>
        /// <returns></returns>
        [HttpGet("{skip}/{take}")]
        public List<TemplatePack> Get(int skip, int take) {
            int numpacks = TemplatePacks.Count;

            if(skip >= numpacks) {
                return null;
            }
            if(take <= 0) {
                take = 50;
            }

            if(skip+take >= numpacks) {
                take = numpacks - skip - 1;
            }

            return new List<TemplatePack>(TemplatePacks.GetRange(skip, take));
        }

        [HttpGet("stats")]
        public TemplateStats GetStats() {
            return new TemplateStats(TemplatePacks);
        }
    }
}

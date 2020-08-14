using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TemplatesShared {
    public class TemplateStats {
        public TemplateStats() : this(null) {

        }
        public TemplateStats(List<TemplatePack> templatePacks) {
            if (templatePacks != null) {
                NumDownloads = templatePacks.Sum(x => { return x.DownloadCount; });
                NumTemplates = templatePacks.Sum(x => x.Templates.Length);
                NumTemplatePacks = templatePacks.Count;
                // I'm pretty sure this is not correct because each string can contain multiple authors.
                NumAuthors = templatePacks.Select(x => x.Authors).Distinct().Count();
            }
        }
        public int NumDownloads { get; set; }
        public int NumTemplates { get; set; }
        public int NumTemplatePacks { get; set; }
        public int NumAuthors { get; set; }
    }
}

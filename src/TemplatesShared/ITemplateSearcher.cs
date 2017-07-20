using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared
{
    public interface ITemplateSearcher
    {
        List<Template> Search(string searchTerm, List<TemplatePack> templatePacks);
    }
}

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TemplatesShared;

namespace TemplatesConsole.Storage {
    public class TemplatesContext : DbContext{
        public DbSet<TemplatePack> TemplatePacks { get; set; }
        public DbSet<Template> Templates { get; set; }
        
    }
}

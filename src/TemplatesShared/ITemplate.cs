using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared
{
    public interface ITemplate
    {
        string Author { get; set; }
        string Name { get; set; }
        Dictionary<string,string> Tags { get; set; }
        string[] Classifications { get; set; }
        string ShortName { get; set; }
        string GroupIdentity { get; set; }
        string Identity { get; set; }
    }

    public interface ITemplatePack
    {
        string Owner { get; set; }
        string Version { get; set; }
        int DownloadCount { get; set; }
        string[] Tags { get; set; }
        ITemplate[] Templates { get; set; }
        string DownloadUrl { get; set; }
        string Description { get; set; }
        string Copyright { get; set; }
        string Authors { get; set; }
        string LicenseUrl { get; set; }
        string ProjectUrl { get; set; }
        string Id { get; set; }
    }
}

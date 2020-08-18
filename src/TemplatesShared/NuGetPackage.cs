using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace TemplatesShared
{
    // class to use to represent a NuGet package when calling nuget apis
    public class NuGetPackage
    {
        [JsonProperty("@Id")]
        public string ApiId { get; set; }
        [JsonProperty("@Type")]
        public string Type { get; set; }

        public string Id { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public NuGetVersion[] Versions { get; set; }
        public string[] Authors { get; set; }
        public string IconUrl { get; set; }
        public string LicenseUrl { get; set; }
        public string[] Owners { get; set; }
        public string ProjectUrl { get; set; }
        public string Registration { get; set; }
        public string Summary { get; set; }
        public string[] Tags { get; set; }
        public string Title { get; set; }
        public int TotalDownloads { get; set; }
        public bool Verified { get; set; }
        public NuGetPackageType[] PackageTypes { get; set; }

        public override string ToString() {
            return $"id={Id}\tTitle={Title}";
        }

        [JsonIgnore]
        internal string LocalFilepath { get; set; }
        [JsonIgnore]
        internal string LocalExtractPath { get; set; }
        internal string GetPackageFilename() {
            // {LOWER_ID}.{LOWER_VERSION}.nupkg
            return $"{Normalize(Id)}.{Normalize(Version)}.nupkg";
        }
        internal string Normalize(string keyStr) {
            return keyStr.ToLowerInvariant();
        }
    }

    public class NuGetVersion
    {
        public string Version { get; set; }
        public int Downloads { get; set; }
        [JsonProperty("@Id")]
        public string ID { get; set; }
    }    
    public class NuGetPackageType
    {
        [XmlAttribute("name")]
        public string Name { get; set; }
    }
}

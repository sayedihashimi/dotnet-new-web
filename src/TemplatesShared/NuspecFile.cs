using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TemplatesShared {
    [XmlRoot("package",Namespace = @"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd")]
    public class NuspecFile {
        [XmlElement("metadata")]
        public NuspecMetadata Metadata { get; set; }

        public static NuspecFile CreateFromNuspecFile(string filepath) {
            if (!File.Exists(filepath)) {
                throw new ArgumentNullException($"Cannot find nuspec file at '{filepath}'");
            }
            var nuspecNs = @"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";

            using var filestream = File.Open(filepath, FileMode.Open);
            var serializer = new XmlSerializer(typeof(NuspecFile));
            var nuspec = (NuspecFile)serializer.Deserialize(filestream);

            return nuspec;
        }

    }
    public class NuspecMetadata {
        [XmlElement("id")]
        public string Id { get; set; }
        [XmlElement("version")]
        public string Version { get; set; }
        [XmlElement("authors")]
        public string Authors { get; set; }
        [XmlElement("owners")]
        public string Owners { get; set; }
        [XmlElement("licenseUrl")]
        public string LicenseUrl { get; set; }
        [XmlElement("projectUrl")]
        public string ProjectUrl { get; set; }
        [XmlElement("iconUrl")]
        public string IconUrl { get; set; }
        [XmlElement("description")]
        public string Description { get; set; }
        [XmlElement("copyright")]
        public string Copyright { get; set; }
        [XmlElement("tags")]
        public string Tags { get; set; }
        [XmlElement("repository")]
        public NuspecRepository Repository { get; set; }
    }
    public class NuspecRepository {
        [XmlAttribute("type")]
        public string Type { get; set; }
        [XmlAttribute("url")]
        public string Url { get; set; }
        [XmlAttribute("commit")]
        public string Commit { get; set; }
    }
}

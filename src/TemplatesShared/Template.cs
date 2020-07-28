using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared
{
    public class Template
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        // TODO: with new json file generator I believe that this attribute is no longer needed. 
        // It should harm much though.
        [JsonConverter(typeof(StringArrayConverter))]
        public string[] Classifications { get; set; }
        public string ShortName { get; set; }
        public string GroupIdentity { get; set; }
        public string Identity { get; set; }
        [JsonIgnore()]
        public int SearchScore { get; set; }
        public string TemplatePackId { get; set; }
    }

    public class TemplatePack
    {
        public string Owners { get; set; }
        public string Version { get; set; }
        public int DownloadCount { get; set; }
        [JsonConverter(typeof(TemplateConverter))]
        public Template[] Templates { get; set; }
        public string DownloadUrl { get; set; }
        public string Description { get; set; }
        private string _copyright;
        public string Copyright {
            get { return _copyright; }
            set {
                if(value != null) {
                    value = value.Replace("?", "©");
                }
                _copyright = value;
            }
        }
        public string Authors { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }

        public string Package { get; set; }
        private string _iconurl;
        public string IconUrl {
            get {
                if(string.IsNullOrWhiteSpace(_iconurl)){
                    return @"/images/NuGet_project_logo.svg";
                }
                return _iconurl; 
            }
            set {
                if (string.IsNullOrWhiteSpace(value)) {
                    value = @"/images/NuGet_project_logo.svg";
                }
                _iconurl = value;
            }
        }

        public string IconPngUrl{
            get{
                string defaultIconUrl = @"/images/NuGet_project_logo.svg";
                if(string.IsNullOrWhiteSpace(IconUrl) || 
                   IconUrl.EndsWith("µ.svg",StringComparison.OrdinalIgnoreCase)){
                    return defaultIconUrl;
                }

                return IconUrl;
            }
        }

        public static List<TemplatePack>CreateFromFile(string filepath)
        {
            string jsonString = System.IO.File.ReadAllText(filepath);

            List<TemplatePack>results = CreateFromText(jsonString);
            // assign the templatepackid if not already set
            foreach(var tp in results) {
                foreach(var t in tp.Templates) {
                    if(string.IsNullOrWhiteSpace(t.Name)){
                        continue; 
                    }
                    if (string.IsNullOrWhiteSpace(t.TemplatePackId)) {
                        t.TemplatePackId = tp.Package;
                    }
                }
            }

            return results;
        }

        public static List<TemplatePack> CreateFromText(string text)
        {
            var result = JsonConvert.DeserializeObject<List<TemplatePack>>(text);

            foreach(var tp in result){
                if (tp.Templates != null){
                    List<Template> tList = tp.Templates.ToList();
                    if(tList.RemoveAll(tr => { return string.IsNullOrWhiteSpace(tr.Name); }) > 0){
                        tp.Templates = tList.ToArray();
                    }

                }
            }

            return result;
        }
        public static async Task<List<TemplatePack>> CreateFromTextAsync(string text)
        {
            var result = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<List<TemplatePack>>(text));

            foreach (var tp in result)
            {
                if (tp.Templates != null)
                {
                    List<Template> tList = tp.Templates.ToList();
                    if (tList.RemoveAll(tr => { return string.IsNullOrWhiteSpace(tr.Name); }) > 0)
                    {
                        tp.Templates = tList.ToArray();
                    }

                }
            }

            return result;
            // return JsonConvert.DeserializeObject<List<TemplatePack>>(text);
        }
        // TODO: Move these methods somewhere else
        public static TemplatePack CreateFromNuSpec(string pathToNuspecFile, List<string>pathToTemplateJsonFiles) {
            Debug.Assert(File.Exists(pathToNuspecFile));
            Debug.Assert(pathToTemplateJsonFiles != null && pathToTemplateJsonFiles.Count > 0);

            var nuspec = NuspecFile.CreateFromNuspecFile(pathToNuspecFile);
            var templateList = new List<Template>();
            foreach(var filepath in pathToTemplateJsonFiles) {
                var template = CreateTemplateFromJsonFile(filepath);
                templateList.Add(template);
            }

            // TODO: get download count
            var templatePack = new TemplatePack {
                Authors = nuspec.Metadata.Authors,
                Copyright = nuspec.Metadata.Copyright,
                Description = nuspec.Metadata.Description,
                IconUrl = nuspec.Metadata.IconUrl,
                LicenseUrl = nuspec.Metadata.LicenseUrl,
                Owners = nuspec.Metadata.Owners,
                ProjectUrl = nuspec.Metadata.ProjectUrl,
                Version = nuspec.Metadata.Version,
                Templates = templateList.ToArray()
            };

            return templatePack;
        }

        public static Template CreateTemplateFromJsonFile(string filepath) {
            if (!File.Exists(filepath)) {
                throw new ArgumentNullException($"Cannot find template json file at '{filepath}");
            }

            return JsonConvert.DeserializeObject<Template>(File.ReadAllText(filepath));
        }
    }
    public class TemplateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Template[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<Template[]>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // not needed
            JToken fromobj = JToken.FromObject(value);
            fromobj.WriteTo(writer);
        }
    }

    public class StringArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(string[]) || objectType == typeof(string));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string[] result = null;

            bool wasSuccess = true;
            try
            {
                result = serializer.Deserialize<string[]>(reader);
                wasSuccess = true;
            }
            catch(Exception)
            {
                wasSuccess = false;
            }

            if(!wasSuccess)
            {
                // see if we can get a string out of it
                try
                {
                    result = new string[1] { serializer.Deserialize<string>(reader) };
                    wasSuccess = true;
                }
                catch(Exception)
                {
                    wasSuccess = false;
                }
            }

            if (!wasSuccess)
            {
                throw new ArgumentException("Unable to create string[] or string from given value");
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);
            t.WriteTo(writer);
        }
    }
}

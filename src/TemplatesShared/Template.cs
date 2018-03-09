using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared
{
    public class Template
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
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
                    return @"https://preview.nuget.org/Content/gallery/img/default-package-icon.svg";
                }
                return _iconurl; 
            }
            set {
                if (string.IsNullOrWhiteSpace(value)) {
                    value = @"https://preview.nuget.org/Content/gallery/img/default-package-icon.svg";
                }
                _iconurl = value;
            }
        }

        public string IconPngUrl{
            get{
                string defaultIconUrl = "http://dotnetnew.azurewebsites.net/images/create-project-64.png";
                if(string.IsNullOrWhiteSpace(IconUrl) || 
                   IconUrl.EndsWith(".svg",StringComparison.OrdinalIgnoreCase)){
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
                    if (string.IsNullOrWhiteSpace(t.TemplatePackId)) {
                        t.TemplatePackId = tp.Package;
                    }
                }
            }

            return results;
        }

        public static List<TemplatePack> CreateFromText(string text)
        {
            return JsonConvert.DeserializeObject<List<TemplatePack>>(text);
        }
        public static async Task<List<TemplatePack>> CreateFromTextAsync(string text)
        {

            return await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<List<TemplatePack>>(text));
            // return JsonConvert.DeserializeObject<List<TemplatePack>>(text);
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
            catch(Exception ex)
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
                catch(Exception ex)
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

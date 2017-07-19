using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared
{
    public class Template : ITemplate
    {
        public string Author { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        [JsonConverter(typeof(StringArrayConverter))]
        public string[] Classifications { get; set; }
        public string ShortName { get; set; }
        public string GroupIdentity { get; set; }
        public string Identity { get; set; }
    }

    public class TemplatePack : ITemplatePack
    {
        public string Owner { get; set; }
        public string Version { get; set; }
        public int DownloadCount { get; set; }
        [JsonConverter(typeof(StringArrayConverter))]
        public string[] Tags { get; set; }
        [JsonConverter(typeof(TemplateConverter))]
        public ITemplate[] Templates { get; set; }
        public string DownloadUrl { get; set; }
        public string Description { get; set; }
        public string Copyright { get; set; }
        public string Authors { get; set; }
        public string LicenseUrl { get; set; }
        public string ProjectUrl { get; set; }
        public string Id { get; set; }
    }


    public class TemplateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ITemplate[]));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<Template[]>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // not needed
            throw new NotImplementedException();
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
            // Not needed
            throw new NotImplementedException();
        }
    }
}

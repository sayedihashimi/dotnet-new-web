using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TemplatesShared.Extensions;

namespace TemplatesShared
{
    public class Template
    {
        public string Author { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(DictionaryConverter))]
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        // TODO: with new json file generator I believe that this attribute is no longer needed. 
        // It should harm much though.
        [JsonConverter(typeof(StringArrayConverter))]
        public string[] Classifications { get; set; }
        [JsonConverter(typeof(StringArrayConverter))]
        public string[] ShortName { get; set; }
        public string GroupIdentity { get; set; }
        public string Identity { get; set; }
        [JsonIgnore()]
        public int SearchScore { get; set; }
        public string TemplatePackId { get; set; }
        public string SourceName { get; set; }
        public string DefaultName { get; set; }
        public string Baseline { get; set; }
        public PrimaryOutput[] PrimaryOutputs { get; set; }

        [JsonIgnore()]
        public List<TemplateSymbolInfo> Symbols { get; set; } = new List<TemplateSymbolInfo>();
        
        // hack to write symbols out to new .json but to not read them on deserialize
        //[JsonProperty("symbols")]
        //public List<TemplateSymbolInfo> SymbolsReadonly {
        //    get {
        //        return Symbols;
        //    }
        //}

        [JsonIgnore()]
        public string LocalFilePath { get; set; }

        [JsonIgnore()]
        public List<TemplateHostFile> HostFiles { get; set; } = new List<TemplateHostFile>();

        /// <summary>
        /// Call this method, to populate the HostFiles property
        /// </summary>
        /// <param name="dirPath">root directory to where the template pack is located</param>
        public void InitHostFilesFrom(string dirPath, string templatePackId, string templateName) {
            HostFiles = new List<TemplateHostFile>();
            var hostFilePaths = TemplateHostFile.GetHostFilesIn(dirPath);
            if (hostFilePaths != null && hostFilePaths.Count > 0) {
                foreach (var hfp in hostFilePaths)
                {
                    var hf = TemplateHostFile.CreateFromFile(hfp, templatePackId, templateName);                    
                    if (hf != null) {
                        hf.TemplateLocalFilePath = LocalFilePath;
                        HostFiles.Add(hf);
                    }
                    else {
                        Console.WriteLine($"WARNING: Unable to read host file at '{hfp}'");
                    }
                }
            }
        }
        /// <summary>
        /// Returns the value of the tag with the key 'type'
        /// </summary>
        /// <returns></returns>
        public string GetTemplateType() {
            return GetTagByKey("type");
        }
        public string GetLanguage() {
            return GetTagByKey("language");
        }
        public string GetTagByKey(string keyname) {
            if (Tags != null && Tags.Keys.Count > 0) {
                foreach (var key in Tags.Keys) {
                    if (string.Compare(keyname, key, StringComparison.OrdinalIgnoreCase) == 0) {
                        return Tags[key];
                    }
                }
            }
            return null;
        }

        public static Template CreateFromFile(string filepath) =>
            CreateFromText(File.ReadAllText(filepath), filepath);

        public static Template CreateFromText(string json, string localFilepath)
        {
            var result = JsonConvert.DeserializeObject<Template>(json);
            if (result != null) {
                result.LocalFilePath = localFilepath;
                result.InitSymbolsFrom(json);
            }
            return result;
        }
        protected void InitSymbolsFrom(string json){
            Debug.Assert(!string.IsNullOrEmpty(json));

            var jobj = JObject.Parse(json);

            this.Symbols = new List<TemplateSymbolInfo>();
            if (jobj.ContainsKey("symbols")){
                var symbols = jobj["symbols"];
                

                foreach(JProperty child in symbols.Children()){
                    var symbolInfo = new TemplateSymbolInfo();
                    foreach(var v in child.Values()) {
                        JProperty value = v as JProperty;
                        if (value == null) {
                            // TODO: Look into why this is happening, may be an issue.
                            continue;
                        }

                        // Console.WriteLine(value);
                        // ((Newtonsoft.Json.Linq.JValue)value.First).Value

                        JValue jv = value.First as JValue;
                        if(jv == null) {
                            // could be things like choices, parameters, etc.
                            continue;
                        }
                        switch (value.Name)
                        {
                            case "type":
                                symbolInfo.Type = jv.Value<string>();
                                break;
                            case "description":
                                symbolInfo.Description = jv.Value<string>();
                                break;
                            case "defaultValue":
                                symbolInfo.DefaultValue = jv.Value<string>();
                                break;
                            case "DataType":
                                symbolInfo.Datatype = jv.Value<string>();
                                break;
                            case "replaces":
                                symbolInfo.Replaces = jv.Value<string>();
                                break;
                            default:
                                break;
                        }
                    }
                    Symbols.Add(symbolInfo);
                }
            }
        }
    }
    public class PrimaryOutput {
        public string Path { get; set; }
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
                return _iconurl; 
            }
            set {
                _iconurl = value;
            }
        }

        public string IconPngUrl{
            get{
                return IconUrl;
            }
        }
        [JsonIgnore()]
        public string LocalFilePath { get; set; }
        public static List<TemplatePack> CreateFromFile(string filepath)
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

            results = results.OrderBy((t) => -1*t.DownloadCount).ToList();

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
        }

        public static Dictionary<string,TemplatePack> ConvertToDictionary(List<TemplatePack> packs) {
            Debug.Assert(packs != null);

            Dictionary<string, TemplatePack> result = new Dictionary<string, TemplatePack>();

            foreach(var pack in packs) {
                string id = NormalisePkgId(pack.Package);
                if (!result.ContainsKey(id)) {
                    result.Add(id, pack);
                }
                else {
                    // TODO: use reporter
                    Console.WriteLine($"pack already in dictionary '{id}'");
                }
            }

            return result;
        }

        public static string NormalisePkgId(string id) {
            Debug.Assert(id != null);

            return id.Trim().ToLowerInvariant();
        }

        // TODO: Move these methods somewhere else
        public static TemplatePack CreateFromNuSpec(NuGetPackage pkg, string pathToNuspecFile, List<string>pathToTemplateJsonFiles) {
            Debug.Assert(pkg != null);
            Debug.Assert(File.Exists(pathToNuspecFile));
            Debug.Assert(pathToTemplateJsonFiles != null && pathToTemplateJsonFiles.Count > 0);

            try {
                var nuspec = NuspecFile.CreateFromNuspecFile(pathToNuspecFile);
                var templateList = new List<Template>();
                foreach (var filepath in pathToTemplateJsonFiles) {
                    Console.WriteLine($"reading template file from {filepath}");
                    try {
                        var template = CreateTemplateFromJsonFile(filepath, pkg.Id);
                        templateList.Add(template);
                    }
                    catch(Exception ex) {
                        Console.WriteLine(ex.ToString());
                        continue;
                    }
                }

                var templatePack = new TemplatePack {
                    Package = nuspec.Metadata.Id,
                    Authors = nuspec.Metadata.Authors,
                    Copyright = nuspec.Metadata.Copyright,
                    Description = nuspec.Metadata.Description,
                    IconUrl = nuspec.Metadata.IconUrl,
                    LicenseUrl = nuspec.Metadata.LicenseUrl,
                    Owners = nuspec.Metadata.Owners,
                    ProjectUrl = nuspec.Metadata.ProjectUrl,
                    Version = nuspec.Metadata.Version,
                    Templates = templateList.ToArray(),
                    DownloadCount = pkg.TotalDownloads
                };

                return templatePack;
            }
            catch(Exception ex) {
                Console.WriteLine(ex.ToString());
                throw ex;
            }
        }

        public static Template CreateTemplateFromJsonFile(string filepath, string templatePackId) {
            if (!File.Exists(filepath)) {
                throw new ArgumentNullException($"Cannot find template json file at '{filepath}");
            }
            
            //var template = JsonConvert.DeserializeObject<Template>(File.ReadAllText(filepath));
            var template = Template.CreateFromFile(filepath);
            // we need to set templatePackId
            template.TemplatePackId = templatePackId;



            return template;
        }
        protected void InitSymbols()
        {

        }
        public static List<string> GetTemplateFilesUnder(string contentDir) {
            Debug.Assert(Directory.Exists(contentDir));

            var templateFolders = Directory.GetDirectories(contentDir, ".template.config", SearchOption.AllDirectories);
            var templateFiles = new List<string>();
            foreach (var folder in templateFolders) {
                var files = Directory.GetFiles(folder, "template.json", new EnumerationOptions { RecurseSubdirectories = true });
                if (files != null && files.Length > 0) {
                    templateFiles.AddRange(files);
                }
            }

            return templateFiles;
        }
    }

    public class TemplateHostFile {
        public string TempaltePackId { get; set; }
        public string TemplateName { get; set; }
        public string Icon { get; set; }
        public string LearnMoreLink { get; set; }
        public List<string> UiFilters { get; set; } = new List<string>();
        public string MinFullFrameworkVersion { get; set; }
        

        [JsonIgnore()]
        public string LocalFilePath { get; set; }
        [JsonIgnore()]
        public string TemplateLocalFilePath { get; set; }

        public static TemplateHostFile CreateFromText(string text, string localFilepath, string templatePackId,string templateName)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            JObject hostFileJo = null;
            try
            {
                hostFileJo = JObject.Parse(text);
            }
            catch(JsonReaderException ex)
            {
                Console.WriteLine("Json error in file '{0}. Error:\n{1}", localFilepath, ex.ToString());
            }

            if (hostFileJo != null)
            {
                var uiFilters = new List<string>();
                if (hostFileJo.ContainsKey("uiFilters"))
                {
                    uiFilters = ((JArray)hostFileJo["uiFilters"]).Select(c => (string)c).ToList();
                }

                return new TemplateHostFile
                {
                    TempaltePackId = templatePackId,
                    TemplateName = templateName,
                    Icon = hostFileJo.GetValueOrDefault<string>("icon", string.Empty),
                    LearnMoreLink = hostFileJo.GetValueOrDefault<string>("learnMoreLink", string.Empty),
                    MinFullFrameworkVersion = hostFileJo.GetValueOrDefault<string>("minFullFrameworkVersion", string.Empty),
                    LocalFilePath = localFilepath,
                    UiFilters = uiFilters
                };
            }
            else
            {
                return null;
            }
        }

        public static TemplateHostFile CreateFromFile(string filepath, string templatePackId = "",string templateName="") =>
            CreateFromText(File.ReadAllText(filepath), filepath, templatePackId, templateName);

        public static List<string> GetHostFilesIn(string contentDir) {
            Debug.Assert(Directory.Exists(contentDir));

            var result = new List<string>();

            var found = Directory.GetFiles(contentDir, "*.host.json", SearchOption.TopDirectoryOnly);

            if(found != null && found.Length > 0) {
                result.AddRange(found);
            }

            return result;
        }
    }

    public class TemplateSymbolInfo {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Datatype { get; set; }
        public string Replaces { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
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
    public class DictionaryConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return (objectType == typeof(Dictionary<string,string>) || objectType == typeof(Dictionary<string, object>));
        }
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var mapStringObj = serializer.Deserialize<Dictionary<string, object>>(reader);
            var result = new Dictionary<string, string>();
            foreach(var key in mapStringObj.Keys) {
                var obj = mapStringObj[key];
                if (obj.GetType() == typeof(string)) {
                    result.Add(key, (string)obj);
                }
            }

            return result;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            throw new NotImplementedException();
        }
    }
}

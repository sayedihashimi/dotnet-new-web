using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
namespace TemplatesShared {
    public interface IJsonSchemaHelper {
        string GetErrorStringFor(ValidationError ve);
        JObject GetJObjectFor(string jsonFilepath);
        string GetSchemaFileFor(string url);
        JSchema GetSchemaFor(string schemFilepath);
        List<ValidationError> Validate(JSchema schema, JObject jobj);
        List<ValidationError> Validate(string schemaPath, string jsonPath);
    }

    public class JsonSchemaHelper : IJsonSchemaHelper {
        protected Dictionary<string, JSchema> _schemaMap { get; set; } = new Dictionary<string, JSchema>();
        protected Dictionary<string, JObject> _objMap { get; set; } = new Dictionary<string, JObject>();
        protected Dictionary<string, string> _shemaUrlFilePathMap { get; set; } = new Dictionary<string, string>();
        protected string tempFilepath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"templatereport/{DateTime.Now.ToString("MM.dd.yy - H.m.s.ffff")}");
        private IReporter _reporter;
        public JsonSchemaHelper(IReporter reporter) {
            Debug.Assert(reporter != null);
            _reporter = reporter;
        }
        public List<ValidationError>Validate(string schemaPath, string jsonPath) {
            ValidateNew(schemaPath, jsonPath);
            return Validate(GetSchemaFor(schemaPath), GetJObjectFor(jsonPath));
        }

        private Manatee.Json.Serialization.JsonSerializer js = new Manatee.Json.Serialization.JsonSerializer();
        public void ValidateNew(string schemaPath, string jsonPath) {

            // Manatee.Json.Schema.JsonSchema js = new Manatee.Json.Schema.JsonSchema()
            //var json = JsonValue.Parse(text);
            //var mySchema = serializer.Deserialize<JsonSchema>(json);

            // load schema
            var schemaText = File.ReadAllText(schemaPath);
            var schemaJson = Manatee.Json.JsonValue.Parse(schemaText);
            var schema = new Manatee.Json.Serialization.JsonSerializer().Deserialize<Manatee.Json.Schema.JsonSchema>(schemaJson);
            // schema.Validate()

            // load json as JsonObject
            try {
                var jValue = Manatee.Json.JsonValue.Parse(File.ReadAllText(jsonPath));
                Manatee.Json.Schema.JsonSchemaOptions jso = new Manatee.Json.Schema.JsonSchemaOptions() {
                    OutputFormat = Manatee.Json.Schema.SchemaValidationOutputFormat.Basic
                };
                var results = schema.Validate(jValue, jso);
                var resultsFlattened = results.Flatten();
                var resultsCondensed = results.Condense();

                if (!results.IsValid) {
                    var errors = (from r in results.NestedResults
                                 where !r.IsValid
                                 select r).ToList();
                    var errorDetails = results.ToJson(js).ToString();

                    Console.WriteLine("delete this");
                }

            }
            catch (Manatee.Json.JsonSyntaxException) {
                _reporter.WriteLine($"ERROR:\tInvalid json file '{jsonPath}'");
                return;
            }
        }

        public List<ValidationError> Validate(JSchema schema, JObject jobj) {
            Debug.Assert(schema != null);
            Debug.Assert(jobj != null);
            IList<ValidationError> errors;
            jobj.IsValid(schema, out errors);

            return errors != null ? errors.ToList() : new List<ValidationError>();
        }

        public JSchema GetSchemaFor(string schemFilepath) {
            if (string.IsNullOrEmpty(schemFilepath)) {
                throw new ArgumentNullException("schemaFilepath cannot be empty");
            }

            var key = NormalizeString(schemFilepath);
            JSchema schema;
            _schemaMap.TryGetValue(key, out schema);

            if (schema == null) {
                schema = JSchema.Parse(File.ReadAllText(schemFilepath));
                _schemaMap.Add(key, schema);
            }

            return schema;
        }

        public JObject GetJObjectFor(string jsonFilepath) {
            if (string.IsNullOrEmpty(jsonFilepath)) {
                throw new ArgumentNullException("json value cannot be null");
            }
            var key = NormalizeString(jsonFilepath);
            JObject result;
            _objMap.TryGetValue(jsonFilepath, out result);
            if(result == null) {
                result = JObject.Parse(File.ReadAllText(jsonFilepath));
                _objMap.Add(key, result);
            }
            return result;
        }

        protected string NormalizeString(string key) {
            return key == null ? null : key.Trim().ToLowerInvariant();
        }

        public string GetSchemaFileFor(string url) {
            if (string.IsNullOrEmpty(url)) {
                throw new ArgumentNullException("url", "url value cannot be empty");
            }
            var key = NormalizeString(url);
            string filepath;
            _shemaUrlFilePathMap.TryGetValue(key, out filepath);
            if (string.IsNullOrEmpty(filepath)) {
                // download to temp folder and then move it to the correct temp folder
                filepath =  Path.Combine(tempFilepath, $"schema.{Guid.NewGuid()}.json");
                CreateDirectoryIfNotExists(Path.GetDirectoryName(filepath));
                new System.Net.WebClient().DownloadFile(url, filepath);
                if (!File.Exists(filepath)) {
                    var msg = $"ERROR: Unable to download file from url '{url}'";
                    _reporter.WriteLine(msg);
                    // TODO: Change exception type
                    throw new ApplicationException(msg);
                }
                _shemaUrlFilePathMap.Add(key, filepath);
            }

            return filepath;          
        }
        public string GetErrorStringFor(ValidationError ve) {
            Debug.Assert(ve != null);
            // c1: [Commandline] [warning|error] [code]:[message]
            return $"ERROR: ({ve.LineNumber},{ve.LinePosition}): {ve.Path}: {ve.Message}";
        }
        private void CreateDirectoryIfNotExists(string dirpath) {
            if (string.IsNullOrEmpty(dirpath)) {
                throw new ArgumentNullException("dirpath", "dirpath cannot be empty");
            }
            if (!Directory.Exists(dirpath)) {
                Directory.CreateDirectory(dirpath);
            }
        }
    }
}

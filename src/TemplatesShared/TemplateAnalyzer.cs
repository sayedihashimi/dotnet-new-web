using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
namespace TemplatesShared {
    /// <summary>
    /// Checks:
    ///      1. Check for required, and recommended, properties in the template.json file
    ///      2. Check for recommended properties
    ///             defaultName, description
    ///      3. Analyze symbols for issues (todo: talk to Phil)
    ///      4. Check for ide.host.json (or similar filename) and for recommended properties
    ///      5. Analyze symbolInfo for issues (todo: talk to Phil)
    /// required properties of the template.json file
    ///  name, sourceName, tags:type==[project,item]
    /// recommended properties of the template.json file
    ///  $schema, defaultName, tags:language,  author, classifications (is it required?), Framework symbol
    /// recommended properties of the ide.host.json file
    ///  $schema, icon
    /// </summary>
    public class TemplateAnalyzer : ITemplateAnalyzer {

        public TemplateAnalyzer(IReporter reporter, IJsonHelper jsonHelper) {
            Debug.Assert(reporter != null);
            Debug.Assert(jsonHelper != null);

            _reporter = reporter;
            _jsonHelper = jsonHelper;
        }

        private IReporter _reporter;
        private IJsonHelper _jsonHelper;
        public void Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            _reporter.WriteLine();
            _reporter.WriteLine($@"Validating '{templateFolder}\.template.config\template.json'");

            string indentPrefix = "    ";
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                _reporter.WriteLine($"ERROR: templateFolder not found at '{templateFolder}'", indentPrefix);
                return;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                _reporter.WriteLine($"template.json not found at '{templateJsonFile}'", indentPrefix);
                return;
            }
            try {
                var jobj = _jsonHelper.LoadJsonFrom(templateJsonFile);
                var foundIssues = CheckTemplateProperties(jobj);
                if (!foundIssues) {
                    _reporter.WriteLine("√ no issues found", indentPrefix);
                }
            }
            catch(Exception ex) {
                _reporter.WriteLine($"ERROR: {ex.ToString()}", indentPrefix);
            }
        }
        /// <summary>
        /// 
        /// Checks for required properties
        ///     author, classifications, identity, name, shortName.
        ///     tags.type
        ///     tags.language
        /// </summary>
        /// <param name="jobj"></param>
        /// <returns>true if errors were detected otherwise false</returns>
        protected bool CheckTemplateProperties(JToken jobj) {
            Debug.Assert(jobj != null);
            string indentPrefix = "    ";

            // check for required properties
            bool foundIssues = false;
            var requiredProps = new List<string> {
                "author",
                "sourceName",
                "classifications",
                "identity",
                "name",
                "shortName",
                "tags"
            };
            
            foreach(var rp in requiredProps) {
                if (!HasValue(jobj[rp])) {
                    WriteOutput($"ERROR: Missing required property: '{rp}'");
                }
            }

            var tagsObj = jobj["tags"];
            JToken typeVal = null;
            JToken langVal = null;
            if(tagsObj != null) {
                langVal = tagsObj["language"];
                typeVal = tagsObj["type"];
            }

            if (!HasValue(langVal)) {
                WriteOutput($"ERROR: Missing required property: 'tags/language'");
            }

            if (!HasValue(typeVal)) {
                WriteOutput($"ERROR: Missing required property: 'tags/type'");
            }
            else {
                var val = ((Newtonsoft.Json.Linq.JValue)(jobj["tags"]["type"])).Value.ToString();

                if (string.Compare("project", val, true) != 0 &&
                    string.Compare("item", val, true) != 0) {
                    WriteOutput($"ERROR: value for tags/type should be 'project' or 'item'. Unknown value used:'{val}'");
                }
            }

            // check for recommended properties: defaultName, description
            var recProps = new List<string> {
                "defaultName",
                "description",
            };

            foreach (var recP in recProps) {
                if (!HasValue(jobj[recP])) {
                    WriteOutput($"WARNING: Missing recommended property: '{recP}'");
                }
            }

            void WriteOutput(string msg) {
                foundIssues = true;
                _reporter.WriteLine(msg, indentPrefix);
            }

            return foundIssues;
        }
        protected bool HasValue(JToken token) {
            if(token == null) {
                return false;
            }

            var value = token as JValue;
            if(value != null) {
                if(value.Value == null) {
                    return false;
                }
                if(value.Value is string) {
                    if (string.IsNullOrEmpty((string)(value.Value))) {
                        return false;
                    }
                }
            }

            var array = token as JArray;
            if(array != null) {
                if(array.Count == 0) {
                    return false;
                }
            }

            return true;
        }
        //protected JObject GetJObjByPath(JObject jObj, string path) {
        //    Debug.Assert(jObj != null);
        //    Debug.Assert(!string.IsNullOrEmpty(path));

        //    if (!path.Contains("/")) {
        //        var firstPath = path.Substring(0, path.IndexOf("/"));

        //    }


        //    throw new NotImplementedException();
        //}
    }


    public interface IJsonHelper {
        JToken LoadJsonFrom(string filepath);
    }

    public class JsonHelper : IJsonHelper {
        private Dictionary<string, JToken> _jsonMap = new Dictionary<string, JToken>();
        public JToken LoadJsonFrom(string filepath) {
            Debug.Assert(File.Exists(filepath));

            var key = NormalizeKey(filepath);
            JToken result;
            _jsonMap.TryGetValue(NormalizeKey(filepath), out result);

            if (result == null) {
                result = JObject.Parse(File.ReadAllText(filepath));
                _jsonMap.Add(key, result);
            }

            return result;
        }

        protected string NormalizeKey(string key) {
            if (string.IsNullOrEmpty(key)) { return key; }

            return string.IsNullOrEmpty(key) ? key : key.Trim().ToLowerInvariant();
        }
    }


    public class TemplatePackAnalyzer {
        // Checks:
        //  1. nuspec file is in the folder specified, if not error and stop
        //  2. nuspec file has all the required properties (todo: figure out all the required props in a nuspec file)
    }

    //public class AnalyzerResult {
    //    public AnalyzerResult() { }
    //    public AnalyzerResult(List<Error> errors, List<Warning> warnings) : base() {
    //        Errors = errors != null ? errors : new List<Error>();
    //        Warnings = warnings != null ? warnings : new List<Warning>();
    //    }
    //    public List<Error> Errors { get; protected set; } = new List<Error>();
    //    public List<Warning> Warnings { get; protected set; } = new List<Warning>();

    //    public bool HasErrors {
    //        get {
    //            return (Errors == null || 
    //                    Errors != null && Errors.Count == 0);
    //        }
    //    }
    //    public bool HasWarnings {
    //        get {
    //            return (Warnings == null ||
    //                    Warnings != null && Warnings.Count == 0);
    //        }
    //    }

    //    public AnalyzerResult Combine(AnalyzerResult other) {
    //        var newResult = new AnalyzerResult();

    //        newResult.Errors.AddRange(Errors);
    //        newResult.Warnings.AddRange(Warnings);

    //        if(other != null) {
    //            newResult.Errors.AddRange(other.Errors);
    //            newResult.Warnings.AddRange(other.Warnings);
    //        }

    //        return newResult;
    //    }
    //}
    //public class ErrorOrWarning {
    //    protected ErrorOrWarning(ErrorWarningType type, string message, string file) {
    //        Type = type;
    //        Message = message;
    //        File = file;
    //    }
    //    public ErrorWarningType Type { get; protected set; }
    //    public string File { get; protected set; }
    //    public string Message { get; protected set; }
    //}
    //public class Error : ErrorOrWarning {
    //    public Error(string message, string file) : base(ErrorWarningType.Error,message,file) {
    //    }
    //}
    //public class Warning : ErrorOrWarning {
    //    public Warning(string message, string file) : base(ErrorWarningType.Error, message, file) {
    //    }
    //}
    //public enum ErrorWarningType {
    //    Error = 1,
    //    Warning = 2,
    //    Info = 3,
    //    Unknown = 1000
    //}
}

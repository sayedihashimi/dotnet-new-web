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
    public class TemplateJsonPathAnalyzer : ITemplateAnalyzer {
        public TemplateJsonPathAnalyzer(IReporter reporter, IJsonHelper jsonHelper) {
            Debug.Assert(reporter != null);
            Debug.Assert(jsonHelper != null);

            _reporter = reporter;
            _jsonHelper = jsonHelper;
        }

        private string _outputPrefix = "    ";
        private IReporter _reporter;
        private IJsonHelper _jsonHelper;

        public void Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            _reporter.WriteLine();
            WriteMessage($@"Validating '{templateFolder}\.template.config\template.json'");

            string indentPrefix = "    ";
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                // _reporter.WriteLine($"ERROR: templateFolder not found at '{templateFolder}'", indentPrefix);
                WriteError($"ERROR: templateFolder not found at '{templateFolder}'", _outputPrefix);
                return;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                // _reporter.WriteLine($"template.json not found at '{templateJsonFile}'", indentPrefix);
                WriteError($"template.json not found at '{templateJsonFile}'", _outputPrefix);
                return;
            }


            JToken template;
            try {
                template = _jsonHelper.LoadJsonFrom(templateJsonFile);
            }
            catch(Exception ex) {
                // TODO: make exception more specific
                WriteError($"Unable to load template from: '{templateJsonFile}'.\n Error: {ex.ToString()}");
                return;
            }

            var foundIssues = false;
            var templateRules = GetRules();
            foreach(var rule in templateRules) {
                if(!ExecuteRule(rule, template)) {
                    foundIssues = true;
                    switch (rule.Severity) {
                        case ErrorWarningType.Error:
                            WriteError(rule.GetErrorMessage(), indentPrefix);
                            break;
                        case ErrorWarningType.Warning:
                            WriteWarning(rule.GetErrorMessage(), indentPrefix);
                            break;
                        default:
                            WriteMessage(rule.GetErrorMessage(), indentPrefix);
                            break;
                            
                    }
                }
            }

            if (!foundIssues) {
                _reporter.WriteLine("√ no issues found", indentPrefix);
            }
        }

        private List<JTokenAnalyzeRule> GetRules() {
            var requiredProps = new List<string> {
                "$.author",
                "$.sourceName",
                "$.classifications",
                "$.identity",
                "$.name",
                "$.shortName",
                "$.tags"
            };
            var recommendedProps = new List<string> {
                "$.defaultName",
                "$.description",
            };

            List<JTokenAnalyzeRule> templateRules = new List<JTokenAnalyzeRule>();

            // required properties
            foreach (var requiredProp in requiredProps) {
                templateRules.Add(new JTokenAnalyzeRule {
                    Query = requiredProp,
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Error
                });
            }
            // recommended properties
            foreach (var recProp in recommendedProps) {
                templateRules.Add(new JTokenAnalyzeRule {
                    Query = recProp,
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Warning
                });
            }

            return templateRules;
        }

        private bool ValidateNotEmptyString(JToken token, string jsonPath) {
            var value = _jsonHelper.GetStringValueFromQuery(token, jsonPath);

            return !string.IsNullOrEmpty(value);
        }

        private void WriteError(string message, string prefix = "") {
            WriteImpl(message, "ERROR: ", prefix);
        }
        private void WriteWarning(string message, string prefix = "") {
            WriteImpl(message, "WARNING", prefix);
        }
        private void WriteMessage(string message, string prefix = "") {
            WriteImpl(message, string.Empty, prefix);
        }
        private void WriteImpl(string message, string typeStr, string prefix) {
            if (string.IsNullOrEmpty(message)) { return; }

            _reporter.Write(prefix);
            if (!string.IsNullOrEmpty(typeStr)) {
                _reporter.Write(typeStr);
                _reporter.Write(": ");
            }

            _reporter.WriteLine(message);
        }
        /// <summary>
        /// Returns true if passed otherwise false
        /// </summary>
        private bool ExecuteRule(JTokenAnalyzeRule rule, JToken template) {
            if( rule == null || 
                !_jsonHelper.HasValue(template) ||
                string.IsNullOrEmpty(rule.Query)) {
                return false;
            }

            var queryResult = template.SelectToken(rule.Query);
            var str = _jsonHelper.GetStringValueFromQuery(template, rule.Query);
            switch (rule.Expectation) {
                case JTokenValidationType.Exists:
                    return queryResult != null;
                case JTokenValidationType.StringNotEmpty:                    
                    return !string.IsNullOrEmpty(str);
                case JTokenValidationType.StringEquals:
                    return string.Compare(rule.Value, str, true) == 0;
                default:
                    throw new ArgumentException($"Unknown value for JTokenValidationType:{rule.Expectation}");
            }
        }
    }

    // query
    // expectation
        // exists
        // string - not empty
        // string - equals a specific value
    // error message (should be able to use current value in error message)

    public enum JTokenValidationType {
        Exists = 1,
        StringNotEmpty = 2,
        StringEquals = 3
    }

    public class JTokenAnalyzeRule {
        public string Query { get; set; }
        public JTokenValidationType Expectation { get; set; }
        public string ErrorMessage { get; set; }
        public string Value { get; set; }
        public ErrorWarningType Severity { get; set; }

        public string GetErrorMessage() {
            return GetErrorMessage(null);
        }
        public string GetErrorMessage(string currentValue) {
            return ErrorMessage != null ?
                string.Format(ErrorMessage, currentValue) :
                Expectation switch {
                    JTokenValidationType.Exists => $"{Query} not found",
                    JTokenValidationType.StringNotEmpty => $"{Query} doesn't exist, or doesn't have a value",
                    JTokenValidationType.StringEquals => $"{Query} should be '{Value}' but is '{currentValue}'"
                };

            //string errorMessage = Expectation switch {
            //    JTokenValidationType.Exists => $"{Query} not found",
            //    JTokenValidationType.StringNotEmpty => $"{Query} doesn't exist, or doesn't have a value",
            //    JTokenValidationType.StringEquals => $"{Query} should be '{Value}' but is '{currentValue}'"
            //};
        }
    }

    public class TemplateAnalyzer : ITemplateAnalyzer {

        public TemplateAnalyzer(IReporter reporter, IJsonHelper jsonHelper) {
            Debug.Assert(reporter != null);
            Debug.Assert(jsonHelper != null);

            _reporter = reporter;
            _jsonHelper = jsonHelper;
        }

        private string _outputPrefix = "    ";
        private IReporter _reporter;
        private IJsonHelper _jsonHelper;

        private void WriteError(string message, string prefix = "") {
            WriteImpl(message, "ERROR: ", prefix);
        }
        private void WriteWarning(string message, string prefix = "") {
            WriteImpl(message, "WARNING", prefix);
        }
        private void WriteMessage(string message, string prefix = "") {
            WriteImpl(message, string.Empty, prefix);
        }
        private void WriteImpl(string message, string typeStr, string prefix) {
            if (string.IsNullOrEmpty(message)) { return; }

            _reporter.Write(prefix);
            if (!string.IsNullOrEmpty(typeStr)) {
                _reporter.Write(typeStr);
                _reporter.Write(": ");
            }

            _reporter.WriteLine(message);
        }

        public void Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            _reporter.WriteLine();
            WriteMessage($@"Validating '{templateFolder}\.template.config\template.json'");

            string indentPrefix = "    ";
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                // _reporter.WriteLine($"ERROR: templateFolder not found at '{templateFolder}'", indentPrefix);
                WriteError($"ERROR: templateFolder not found at '{templateFolder}'", _outputPrefix);
                return;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                // _reporter.WriteLine($"template.json not found at '{templateJsonFile}'", indentPrefix);
                WriteError($"template.json not found at '{templateJsonFile}'", _outputPrefix);
                return;
            }
            try {
                var jobj = _jsonHelper.LoadJsonFrom(templateJsonFile);
                var foundIssues = CheckTemplateProperties(jobj);

                foundIssues = CheckSymbols(jobj) || foundIssues;

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
                if (!_jsonHelper.HasValue(jobj[rp])) {
                    // WriteOutput($"ERROR: Missing required property: '{rp}'");
                    WriteError($"Missing required property: '{rp}'");
                }
            }

            var tagsObj = jobj["tags"];
            JToken typeVal = null;
            JToken langVal = null;
            if(tagsObj != null) {
                langVal = tagsObj["language"];
                typeVal = tagsObj["type"];
            }

            if (!_jsonHelper.HasValue(langVal)) {
                // WriteOutput($"ERROR: Missing required property: 'tags/language'");
                WriteError($"Missing required property: 'tags/language'");
            }

            if (!_jsonHelper.HasValue(typeVal)) {
                // WriteOutput($"ERROR: Missing required property: 'tags/type'");
                WriteError($"Missing required property: 'tags/type'");
            }
            else {
                var val = _jsonHelper.GetStringValueFromQuery(jobj, "tags.type");

                if (string.Compare("project", val, true) != 0 &&
                    string.Compare("item", val, true) != 0) {
                    // WriteOutput($"ERROR: value for tags/type should be 'project' or 'item'. Unknown value used:'{val}'");
                    WriteError($"value for tags/type should be 'project' or 'item'. Unknown value used:'{val}'");
                }
            }

            // check for recommended properties: defaultName, description
            var recProps = new List<string> {
                "defaultName",
                "description",
            };

            foreach (var recP in recProps) {
                if (!_jsonHelper.HasValue(jobj[recP])) {
                    // WriteOutput($"WARNING: Missing recommended property: '{recP}'");
                    WriteWarning($"Missing recommended property: '{recP}'");
                }
            }

            void WriteError(string msg) {
                foundIssues = true;
                this.WriteError(msg, _outputPrefix);
            }
            void WriteWarning(string message) {
                foundIssues = true;
                this.WriteWarning(message, _outputPrefix);
            }
            return foundIssues;
        }

        protected bool CheckSymbols(JToken template) {
            bool foundIssues = false;
            if (!_jsonHelper.HasValue(template) || 
                !_jsonHelper.HasValue(template["symbols"])) {
                WriteWarning($"symbols property not found");
            }

            // 1: Check for Framework symbol
            JToken fxSymbol = template != null ? template["symbols"]?["Framework"] : null;
            if (_jsonHelper.HasValue(fxSymbol)) {
                foundIssues = CheckSymbolFramework(fxSymbol) || foundIssues;
            }
            else {
                WriteWarning($"symbols.Framework property is not found.");
            }

            void WriteError(string message) {
                foundIssues = true;
                this.WriteError(message, _outputPrefix);
            }
            void WriteWarning(string message) {
                foundIssues = true;
                this.WriteWarning(message, _outputPrefix);
            }

            return foundIssues;
        }

        protected bool CheckSymbolFramework(JToken fxSymbol) {
            if(!_jsonHelper.HasValue(fxSymbol)) {
                WriteWarning("WARNING: Framework symbol not found");
                return true; 
            }

            bool foundIssues = false;
            var type = fxSymbol["type"];
            if(_jsonHelper.HasValue(type)) {                
                // var typeVal = fxSymbol != null ? ((Newtonsoft.Json.Linq.JValue)(fxSymbol["type"])).Value.ToString().Trim() : null;

                var typeVal = ((JValue)(fxSymbol["type"]))?.Value?.ToString()?.Trim();
                var foo = _jsonHelper.GetStringValueFromQuery(fxSymbol, "type");
                if( typeVal == null ||
                    string.Compare("parameter", typeVal, true) != 0) {
                    WriteWarning($"symbols.Framework.type should be set to 'parameter' but it is set to '{typeVal}'");
                }
            }
            else {
                WriteWarning($"symbols.Framework.type not fund");
            }
            
            var dataType = fxSymbol["datatype"];
            if (_jsonHelper.HasValue(dataType)) {
                var dataTypeVal = ((Newtonsoft.Json.Linq.JValue)(fxSymbol["datatype"]))?.Value?.ToString()?.Trim();
                if( dataTypeVal == null ||
                    string.Compare("choice", dataTypeVal, true) != 0) {
                    WriteWarning($"symbols.Framework.datatype should be set to 'choice', but it is set to '{dataTypeVal}'");
                }
            }
            else {
                WriteWarning("symbols.Framework.datatype not found");
            }
            var choices = fxSymbol["choices"];
            var defaultVale = fxSymbol["defaultValue"];

            void WriteError(string message) {
                foundIssues = true;
                this.WriteError(message, _outputPrefix);
            }
            void WriteWarning(string message) {
                foundIssues = true;
                this.WriteWarning(message, _outputPrefix);
            }

            return foundIssues;
        }
    }

    public enum ErrorWarningType {
        Error = 1,
        Warning = 2,
        Info = 3,
        Unknown = 1000
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

}

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

        /// <summary>
        /// Returns true if issues are found, and false otherwise.
        /// </summary>
        public bool Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            _reporter.WriteLine();
            WriteMessage($@"Validating '{templateFolder}\.template.config\template.json'");

            string indentPrefix = "    ";
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                // _reporter.WriteLine($"ERROR: templateFolder not found at '{templateFolder}'", indentPrefix);
                WriteError($"ERROR: templateFolder not found at '{templateFolder}'", _outputPrefix);
                return true;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                // _reporter.WriteLine($"template.json not found at '{templateJsonFile}'", indentPrefix);
                WriteError($"template.json not found at '{templateJsonFile}'", _outputPrefix);
                return true;
            }

            JToken template;
            try {
                template = _jsonHelper.LoadJsonFrom(templateJsonFile);
            }
            catch(Exception ex) {
                // TODO: make exception more specific
                WriteError($"Unable to load template from: '{templateJsonFile}'.\n Error: {ex.ToString()}");
                return true;
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

            return foundIssues;
        }

        private List<JTokenAnalyzeRule> GetRules() {
            List<JTokenAnalyzeRule> templateRules = new List<JTokenAnalyzeRule>();

            // check required properties
            var requiredProps = new List<string> {
                "$.author",
                "$.sourceName",
                "$.classifications",
                "$.identity",
                "$.name",
                "$.shortName",
                "$.tags",
                "$.tags.language",
                "$.tags.type"
            };
            foreach (var requiredProp in requiredProps) {
                templateRules.Add(new JTokenAnalyzeRule {
                    Query = requiredProp,
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Error
                });
            }

            // $.tags.type should be 'project' or 'item'
            templateRules.Add(new JTokenAnalyzeRule {
                Expectation = JTokenValidationType.Custom,
                Query = "$.tags.type",
                Rule = (currentValue) => {
                    string currentResult = _jsonHelper.HasValue(currentValue as JToken) ?
                        _jsonHelper.GetStringValue(currentValue as JToken) :
                        null;
                    if (string.Compare("project", currentResult, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare("item", currentResult, StringComparison.InvariantCultureIgnoreCase) == 0) {
                        return true;
                    }
                    return false;
                },
                ErrorMessage = $"ERROR: $.tags.type should be either 'project' or 'item'"
            });

            // check recommended properties
            var recommendedProps = new List<string> {
                "$.defaultName",
                "$.description",
                "$.symbols",
                "$.symbols.Framework",
                "$.symbols.Framework.choices"
            };
            foreach (var recProp in recommendedProps) {
                templateRules.Add(new JTokenAnalyzeRule {
                    Query = recProp,
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Warning
                });
            }
            templateRules.Add(new JTokenAnalyzeRule {
                Query = "$.symbols.Framework.type",
                Expectation = JTokenValidationType.StringEquals,
                Value = "parameter",
                ErrorMessage = "WARNING: $.symbols.Framework.type should be 'parameter'"
            });
            templateRules.Add(new JTokenAnalyzeRule {
                Query = "$.symbols.Framework.datatype",
                Expectation = JTokenValidationType.StringEquals,
                Value = "choice",
                ErrorMessage = "WARNING: $.symbols.Framework.datatype should be 'choice'"
            });

            return templateRules;
        }

        private bool ValidateNotEmptyString(JToken token, string jsonPath) {
            var value = _jsonHelper.GetStringValueFromQuery(token, jsonPath);

            return !string.IsNullOrEmpty(value);
        }

        private void WriteError(string message, string prefix = "") {
            WriteImpl(message, "ERROR", prefix);
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
                case JTokenValidationType.Custom:
                    //string currentResult = _jsonHelper.HasValue(queryResult) ?
                    //    _jsonHelper.GetStringValue(queryResult) :
                    //    null;

                    var result = rule.Rule(queryResult);
                    return result;
                default:
                    throw new ArgumentException($"Unknown value for JTokenValidationType:{rule.Expectation}");
            }
        }
    }

    public enum JTokenValidationType {
        Custom,
        Exists,
        StringNotEmpty,
        StringEquals 
    }

    public class JTokenAnalyzeRule {
        public string Query { get; set; }
        public JTokenValidationType Expectation { get; set; }
        public string ErrorMessage { get; set; }
        public string Value { get; set; }
        public ErrorWarningType Severity { get; set; }
        public Func<object,bool> Rule { get; set; }
        public string GetErrorMessage() {
            return GetErrorMessage(null);
        }
        public string GetErrorMessage(string currentValue) =>
            ErrorMessage != null ?
                string.Format(ErrorMessage, currentValue) :
                Expectation switch {
                    JTokenValidationType.Exists => $"{Query} not found",
                    JTokenValidationType.StringNotEmpty => $"{Query} doesn't exist, or doesn't have a value",
                    JTokenValidationType.StringEquals => $"{Query} should be '{Value}'",
                    JTokenValidationType.Custom => $"{Query} failed. Current value: '{currentValue}'",
                    // 0 => throw new ArgumentException($"Unknown value for JTokenValidationType: {Expectation}")
                };
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
}

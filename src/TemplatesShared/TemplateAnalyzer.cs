using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using TemplatesShared.Extensions;
using System.Text.RegularExpressions;

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
        private string _isProjectTemplateRegex = @"""type""\s*:\s*""project""";
        private string _isItemTempalteRegex = @"""type""\s*:\s*""item""";
        private string _isSolutionTemplateRegex = @"""type""\s*:\s*""solution""";

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
                WriteError($"templateFolder not found at '{templateFolder}'", _outputPrefix);
                return true;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                WriteError($"template.json not found at '{templateJsonFile}'", _outputPrefix);
                return true;
            }

            JToken template;
            try {
                template = _jsonHelper.LoadJsonFrom(templateJsonFile);
            }
            catch (Exception ex) {
                // TODO: make exception more specific
                WriteError($"Unable to load template from: '{templateJsonFile}'.\n Error: {ex.ToString()}");
                return true;
            }

            var foundIssues = false;
            var templateType = GetTemplateType(templateJsonFile);
            if(templateType == TemplateType.Unknown) {
                WriteWarning($"Unable to determine if the template is for a project or item (file), assuming it is a project template", indentPrefix);
            }
            WriteVerboseLine($"Found a template of type: '{templateType}'", indentPrefix);
            var templateRules = GetTemplateRules(templateType);
            foreach (var rule in templateRules) {
                if (!ExecuteRule(rule, template)) {
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

            foundIssues = AnalyzeHostFiles(Path.GetDirectoryName(templateJsonFile), indentPrefix) || foundIssues;

            foundIssues = AnalyzeCasingForCommonProperties(template, indentPrefix) || foundIssues;

            if (!foundIssues) {
                _reporter.WriteLine("√ no issues found", indentPrefix);
            }

            return foundIssues;
        }

        private TemplateType GetTemplateType(string templateJsonFilepath) {
            var text = _jsonHelper.GetJsonFromFile(templateJsonFilepath);
            if (string.IsNullOrEmpty(text)) {
                throw new ArgumentException($"Unable to get json from file '{templateJsonFilepath}'");
            }

            bool isProjectTemplate = Regex.IsMatch(text, _isProjectTemplateRegex);
            bool isItemTemplate = Regex.IsMatch(text, _isItemTempalteRegex);
            bool isSolutionTemplate = Regex.IsMatch(text, _isSolutionTemplateRegex);
            if (isSolutionTemplate && !isProjectTemplate && !isItemTemplate) {
                return TemplateType.Solution;
            }
            if(isProjectTemplate && !isItemTemplate && !isSolutionTemplate) {
                return TemplateType.Project;
            }
            else if(!isProjectTemplate && isItemTemplate && !isSolutionTemplate) {
                return TemplateType.Item;
            }
            else {
                throw new JsonException($"Unable to determine if the template if the template is a project template or an item template '{templateJsonFilepath}'");
            }
        }

        /// <summary>
        /// Things this checks for:
        ///  - One or more IDE host files exist
        ///  - Icon is present in all IDE host files
        ///  TODO: Check that the icon file listed is on disk and in, or below, the 
        ///        .template.config folder
        /// </summary>
        protected bool AnalyzeHostFiles(string templateConfigFolder, string indentPrefix) {
            Debug.Assert(!string.IsNullOrEmpty(templateConfigFolder));
            Debug.Assert(indentPrefix != null);
            _reporter.WriteVerbose($"Looking for host files in folder '{templateConfigFolder}'");
            _reporter.WriteVerboseLine();

            var hostFiles = Directory.GetFiles(templateConfigFolder, "*.host.json");
            if(hostFiles == null || hostFiles.Length == 0) {
                //_reporter.WriteLine($"WARNING: ", indentPrefix);
                WriteWarning($"no host files found", indentPrefix);
                return false;
            }
            bool foundIssues = false;

            // check for either a ide.host.json or vs-2017.3.host.json
            var foundAnIdeHostFile = false;
            var hostFileRules = GetHostFileRules();
            foreach(var hf in hostFiles) {
                if (IsAnIdeHostFile(hf)) { foundAnIdeHostFile = true; }
                // check that the icon attribute is included in the host file

                JToken jtoken;
                try {
                    jtoken = _jsonHelper.LoadJsonFrom(hf);
                }
                catch (Exception ex) {
                    // TODO: make exception more specific
                    WriteError($"Unable to load host file from: '{hf}'.\n Error: {ex.ToString()}");
                    continue;
                }

                foreach(var rule in hostFileRules) {
                    foundIssues = !ExecuteRule(rule, jtoken) || foundIssues;
                }
            }

            if (!foundAnIdeHostFile) {
                WriteWarning($"no host file found in folder '{templateConfigFolder}'");
            }

            void WriteError(string text) {
                this.WriteError(text, indentPrefix);
                foundIssues = true;
            }

            return foundIssues;
        }

        protected List<JTokenAnalyzeRule> GetHostFileRules() =>
            new List<JTokenAnalyzeRule> {
                new JTokenAnalyzeRule {
                    Query = "$.icon",
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Error
                }
            };
        
        protected bool IsAnIdeHostFile(string filepath) =>
            new FileInfo(filepath).Name.ToLowerInvariant() switch {
                "ide.host.json" => true,
                "vs-2017.3.host.json" => true,
                _ => false
            };

        protected bool AnalyzeCasingForCommonProperties(JToken template, string indentPrefix) {
            if(template == null) {
                return true;
            }

            var namesToCheck = new List<string> {
                "author",
                "classifications",
                "name",
                "defaultName",
                "identity",
                "shortName",
                "tags",
                "sourceName",
                "preferNameDirectory",
                "sources",
                "baselines",
                "description",
                "groupIdentity",
                "guids",
                "postActions",
                "forms",
                "generatorVersions",
                "placeholderFilename",
                "precedence",
                "primaryOutputs",
                "thirdPartyNotices"
            };

            bool foundIssues = false;
            foreach(var propertyToken in template.Children()) {
                var path = propertyToken.Path;
                if (!string.IsNullOrEmpty(path)) {
                    foreach (var name in namesToCheck) {
                        // check to see if strings match when case insensitive but not when case sensitive
                        if( string.Equals(name, path, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(name, path, StringComparison.Ordinal)) {
                            WriteWarning($"'{path}' should be '{name}', incorrect casing", indentPrefix);
                            foundIssues = true;
                        }
                    }
                }
            }

            return foundIssues;
        }

        protected List<JTokenAnalyzeRule> GetTemplateRules(TemplateType templateType) {
            List<JTokenAnalyzeRule> templateRules = new List<JTokenAnalyzeRule>();
            // check required properties
            var requiredProps = new List<string> {
                "$.author",
                "$.classifications",
                "$.identity",
                "$.name",
            };

            if(templateType == TemplateType.Project) {
                requiredProps.AddRange(new string[] {
                    "$.sourceName",
                    "$.shortName",
                    "$.tags",
                    "$.tags.language",
                    "$.tags.type"
                });
            }

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
                Rule = (jtoken, currentValue) => {
                    string currentResult = _jsonHelper.HasValue(currentValue as JToken) ?
                        _jsonHelper.GetStringValue(currentValue as JToken) :
                        null;
                    if (string.Compare("project", currentResult, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare("item", currentResult, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare("solution", currentResult, StringComparison.InvariantCultureIgnoreCase) == 0) {
                        return true;
                    }
                    return false;
                },
                ErrorMessage = $"ERROR: $.tags.type should be either 'project','item' or 'solution'"
            });

            // check recommended properties
            var recommendedProps = new List<string> {
                "$.defaultName",
                "$.description",
            };
            if(templateType == TemplateType.Project) {
                recommendedProps.AddRange(new string[] {
                    "$.symbols",
                    "$.symbols.Framework",
                    "$.symbols.Framework.choices"
                });
            }
            foreach (var recProp in recommendedProps) {
                templateRules.Add(new JTokenAnalyzeRule {
                    Query = recProp,
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Warning
                });
            }

            if (templateType == TemplateType.Project) {
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
            }

            // ensure primaryOutputs doesn't start with a / or \
            templateRules.Add(new JTokenAnalyzeRule {
                Query = @"$.primaryOutputs",
                Expectation = JTokenValidationType.Custom,
                Rule = (jtoken, currentValue) => {
                    // need to loop through each path value
                    var paths = currentValue as JArray;
                    var errorFiles = new List<string>();
                    if (paths != null) {

                        foreach(var item in paths.Children<JObject>()) {
                            string strResult = _jsonHelper.GetStringValueFromQuery(item, "path");
                            if (!string.IsNullOrWhiteSpace(strResult) &&
                                (strResult.StartsWith(@"/") || strResult.StartsWith(@"\"))) {
                                errorFiles.Add(strResult);
                            }
                        }
                    }

                    if (errorFiles.Count > 0) {
                        return false;
                    }

                    return true;
                },
                ErrorMessage = @"ERROR: One or more $.primaryOutputs.path values starts with a '/' or '\'. You will need to remove that to get the template to work correctly."
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
        private void WriteVerboseLine(string message, string prefix = "") {
            if (string.IsNullOrEmpty(message)) { return; }
            _reporter.WriteVerbose(prefix);
            _reporter.WriteVerboseLine(message, true);
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
        private bool ExecuteRule(JTokenAnalyzeRule rule, JToken jsonToken) {
            if (rule == null ||
                !_jsonHelper.HasValue(jsonToken) ||
                string.IsNullOrEmpty(rule.Query)) {
                return false;
            }

            var queryResult = jsonToken.SelectToken(rule.Query);
            var str = _jsonHelper.GetStringValueFromQuery(jsonToken, rule.Query);
            switch (rule.Expectation) {
                case JTokenValidationType.Exists:
                    return queryResult != null;
                case JTokenValidationType.StringNotEmpty:
                    return !string.IsNullOrEmpty(str);
                case JTokenValidationType.StringEquals:
                    return string.Compare(rule.Value, str, true) == 0;
                case JTokenValidationType.Custom:
                    var result = rule.Rule(jsonToken, queryResult);
                    return result;
                default:
                    throw new ArgumentException($"Unknown value for JTokenValidationType:{rule.Expectation}");
            }
        }
    }

    public enum TemplateType {
        Project = 1,
        Item = 2,
        Solution = 4,
        Unknown = 8
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
        public Func<JToken, object, bool> Rule { get; set; }
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

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
        private AnalyzeResult GetResultFrom(FoundIssue issue) {
            return GetResultFromErrorMessage(issue.IssueType, issue.IssueMessage);
        }
        private AnalyzeResult GetResultFromErrorMessage(ErrorWarningType type, string message) {
            var result = new AnalyzeResult();
            result.Issues.Add(new FoundIssue {
                IssueType = type,
                IssueMessage = message
            });
            return result;
        }
        /// <summary>
        /// Returns true if issues are found, and false otherwise.
        /// </summary>
        public AnalyzeResult Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            
            _reporter.WriteLine("\n");
            WriteMessage($@"Validating '{templateFolder}\.template.config\template.json'");

            string indentPrefix = "    ";
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                WriteError($"templateFolder not found at '{templateFolder}'", _outputPrefix);
                return GetResultFromErrorMessage(ErrorWarningType.Error, $"{_outputPrefix}templateFolder not found at '{templateFolder}'");
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                WriteWarning($"template.json not found at '{templateJsonFile}'", _outputPrefix);
                return GetResultFromErrorMessage(ErrorWarningType.Warning, $"{_outputPrefix}template.json not found at '{templateJsonFile}'");
            }

            JToken template;
            try {
                template = _jsonHelper.LoadJsonFrom(templateJsonFile);
            }
            catch (Exception ex) {
                // TODO: make exception more specific
                WriteError($"Unable to load template from: '{templateJsonFile}'.\n Error: {ex.ToString()}");
                return GetResultFromErrorMessage(ErrorWarningType.Error, $"Unable to load template from: '{templateJsonFile}'.\n Error: {ex.ToString()}");
            }

            var templateType = GetTemplateType(templateJsonFile);
            if(templateType == TemplateType.Unknown) {
                WriteWarning($"Unable to determine if the template is for a project, or item (file) or solution. Assuming it is a project template", indentPrefix);
            }
            WriteVerboseLine($"Found a template of type: '{templateType}'", indentPrefix);
            var templateRules = GetTemplateRules(templateType, template);
            var analyzeResult = new AnalyzeResult();
            foreach (var rule in templateRules) {
                if (!ExecuteRule(rule, template)) {
                    analyzeResult.Issues.Add(new FoundIssue() {
                        IssueType = rule.Severity,
                        IssueMessage = $"{indentPrefix} {rule.GetErrorMessage()}"
                    });
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
            analyzeResult = AnalyzeResult.Combine(analyzeResult, AnalyzeHostFiles(Path.GetDirectoryName(templateJsonFile), indentPrefix));
            analyzeResult = AnalyzeResult.Combine(analyzeResult, AnalyzeCasingForCommonProperties(template, indentPrefix));
            analyzeResult = AnalyzeResult.Combine(analyzeResult, ValidateFilePathsInSources(template, templateFolder));

            if (!analyzeResult.FoundIssues) {
                _reporter.WriteLine("√ no issues found", indentPrefix);
            }

            return analyzeResult;
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
                _reporter.WriteVerboseLine($"Unable to determine if the template if the template is a project template or an item template '{templateJsonFilepath}'");
                return TemplateType.Unknown;
            }
        }

        /// <summary>
        /// Things this checks for:
        ///  - One or more IDE host files exist
        ///  - Icon is present in all IDE host files
        ///  TODO: Check that the icon file listed is on disk and in, or below, the 
        ///        .template.config folder
        /// </summary>
        protected AnalyzeResult AnalyzeHostFiles(string templateConfigFolder, string indentPrefix) {
            Debug.Assert(!string.IsNullOrEmpty(templateConfigFolder));
            Debug.Assert(indentPrefix != null);
            _reporter.WriteVerboseLine($"Looking for host files in folder '{templateConfigFolder}'");

            var analyzeResult = new AnalyzeResult();

            var hostFiles = Directory.GetFiles(templateConfigFolder, "*.host.json");
            
            // TODO: host files are only needed for vs2019.
            //if(hostFiles == null || hostFiles.Length == 0) {
            //    WriteWarning($"no host files found", indentPrefix);
            //    return false;
            //}
            
            // check for either a ide.host.json or vs-2017.3.host.json
            var hostFileRules = GetHostFileRules();
            foreach(var hf in hostFiles) {
                JToken jtoken;
                try {
                    jtoken = _jsonHelper.LoadJsonFrom(hf);
                }
                catch (Exception ex) {
                    // TODO: make exception more specific
                    WriteError($"Unable to load host file from: '{hf}'.\n Error: {ex.ToString()}");
                    analyzeResult.Issues.Add(
                        new FoundIssue {
                            IssueType = ErrorWarningType.Error,
                            IssueMessage = ex.ToString()
                        });
                    continue;
                }

                foreach(var rule in hostFileRules) {
                    if (!ExecuteRule(rule, jtoken)) {
                        analyzeResult.Issues.Add(new FoundIssue {
                            IssueType = rule.Severity,
                            IssueMessage = rule.GetErrorMessage()
                        });
                    }
                }
            }

            // TODO: Host files are only needed in VS2019
            //if (!foundAnIdeHostFile) {
            //    WriteWarning($"no host file found in folder '{templateConfigFolder}'");
            //}

            void WriteError(string text) {
                this.WriteError(text, indentPrefix);
            }

            return analyzeResult;
        }

        protected List<JTokenAnalyzeRule> GetHostFileRules() =>
            new List<JTokenAnalyzeRule> {
                new JTokenAnalyzeRule {
                    Query = "$.icon",
                    Expectation = JTokenValidationType.Exists,
                    Severity = ErrorWarningType.Warning
                }
            };
        
        protected bool IsAnIdeHostFile(string filepath) =>
            new FileInfo(filepath).Name.ToLowerInvariant() switch {
                "ide.host.json" => true,
                "vs-2017.3.host.json" => true,
                _ => false
            };

        protected AnalyzeResult AnalyzeCasingForCommonProperties(JToken template, string indentPrefix) {
            if(template == null) {
                // TODO: Investigate this, we shouldn't be getting into this code
                return new AnalyzeResult();
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

            var analyzeResult = new AnalyzeResult();
            foreach(var propertyToken in template.Children()) {
                var path = propertyToken.Path;
                if (!string.IsNullOrEmpty(path)) {
                    foreach (var name in namesToCheck) {
                        // check to see if strings match when case insensitive but not when case sensitive
                        if( string.Equals(name, path, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(name, path, StringComparison.Ordinal)) {
                            analyzeResult.Issues.Add(new FoundIssue {
                                IssueType = ErrorWarningType.Warning,
                                IssueMessage = $"{indentPrefix}'{path}' should be '{name}', incorrect casing"
                            });
                        }
                    }
                }
            }

            return analyzeResult;
        }

        protected List<JTokenAnalyzeRule> GetTemplateRules(TemplateType templateType, JToken template = null) {
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
                "$.description",
            };
            if(templateType == TemplateType.Project) {
                recommendedProps.AddRange(new string[] {
                    "$.defaultName"
                    // Framework symbol no longer needed, not needed in VS2019 either
                    //"$.symbols",
                    //"$.symbols.Framework",
                    //"$.symbols.Framework.choices"
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
                if (template != null && HasFrameworkSymbolDefined(template)) {
                    templateRules.Add(new JTokenAnalyzeRule {
                        Query = "$.symbols.Framework.type",
                        Expectation = JTokenValidationType.StringEquals,
                        Value = "parameter",
                        ErrorMessage = "WARNING: $.symbols.Framework.type should be 'parameter'",
                        Severity = ErrorWarningType.Warning
                    });
                    templateRules.Add(new JTokenAnalyzeRule {
                        Query = "$.symbols.Framework.datatype",
                        Expectation = JTokenValidationType.StringEquals,
                        Value = "choice",
                        ErrorMessage = "WARNING: $.symbols.Framework.datatype should be 'choice'",
                        Severity = ErrorWarningType.Warning
                    });
                }

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
                ErrorMessage = @"ERROR: One or more $.primaryOutputs.path values starts with a '/' or '\'. You will need to remove that to get the template to work correctly.",
                Severity= ErrorWarningType.Error
            });

            return templateRules;
        }
        private List<string> GetFilePathsFromSources(JToken templateToken) {
            if (templateToken == null) {
                throw new ArgumentNullException(nameof(templateToken));
            }
            var queries = new List<string>() {
                "$.sources.[*].modifiers.[*].exclude",
                "$.sources.[*].modifiers.[*].rename",
                "$.sources.[*].copyOnly"
            };
            var foundFilePaths = new List<string>();

            foreach (var query in queries) {
                var qResult = templateToken.SelectToken(query);
                if (qResult != null) {
                    foreach (var subresult in qResult) {
                        if (subresult != null && subresult.HasValues) {
                            foreach (var v in subresult.Values()) {
                                var str = (v as JValue)?.Value as string;

                                if (!string.IsNullOrEmpty(str)) {
                                    foundFilePaths.Add(str);
                                }
                                else {
                                    _reporter.WriteVerbose($"file path value is null for query '{query}'");
                                }
                            }
                        }
                    }
                }
            }
            return foundFilePaths;
        }
        
        private AnalyzeResult ValidateFilePathsInSources(JToken template, string templateFolder) {
            // paths can be pointing to either files or directories
            // paths can also contain globbing characters
            var analyzeResult = new AnalyzeResult();
            var pathsFound = new List<string>();
            try {
                var queryResult = template.SelectTokens("$.sources.[*].modifiers.[*].rename");
                if (queryResult != null) {
                    foreach(JObject r in queryResult) {
                        if (r.HasValues) {
                            foreach(var ct in r.Children()) {
                                foreach(var child in ct) {
                                    // seems strange to do child.Parent, but I couldn't find a better way
                                    var cp = child.Parent as JProperty;
                                    if(cp != null) {
                                        if (!string.IsNullOrEmpty(cp.Name)) {
                                            pathsFound.Add(cp.Name);
                                        }
                                        else {
                                            analyzeResult.Issues.Add(new FoundIssue { 
                                                IssueType = ErrorWarningType.Warning,
                                                IssueMessage = $"WARNING: no file path found when trying to verify sources paths"
                                            });
                                            _reporter.WriteLine($"WARNING: no file path found when trying to verify sources paths");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                analyzeResult.Issues.Add(new FoundIssue {
                    IssueType = ErrorWarningType.Error,
                    IssueMessage = ex.ToString()
                });
                Console.WriteLine($"ERROR: {ex.ToString()}");
            }
            List<string> missingFiles = new List<string>();
            if (pathsFound != null && pathsFound.Count > 0) {
                // we need to now validate the file paths to ensure they are on disk in the expected location
                //  Directory paths should end with a slash, otherwise it should be a file path
                // set the location to the .template.config folder
                try {
                    var findResult = DoThesePathsExistOnDisk(templateFolder, pathsFound);
                    if (findResult.Exists != null && findResult.MissingPaths.Count > 0) {
                        foreach (var missingPath in findResult.MissingPaths) {
                            analyzeResult.Issues.Add(new FoundIssue {
                                IssueType = ErrorWarningType.Warning,
                                IssueMessage = $"    WARNING: Missing path: '{missingPath}'"
                            });
                            _reporter.WriteLine($"    WARNING: Missing path: '{missingPath}'");
                        }
                    }
                    // return !findResult.Item1;
                }
                catch(Exception ex) {
                    analyzeResult.Issues.Add(new FoundIssue {
                        IssueType = ErrorWarningType.Error,
                        IssueMessage = $"ERROR: {ex.ToString()}"
                    });
                    _reporter.WriteLine($"ERROR: {ex.ToString()}");
                }
            }

            return analyzeResult;
        }
        /// <summary>
        /// Will return true if all paths exist on disk.
        /// </summary>
        private (bool Exists, List<string>MissingPaths) DoThesePathsExistOnDisk(string pwd, List<string> paths) {
            var missingPaths = new List<string>();
            if (paths == null || paths.Count <= 0) {
                throw new ArgumentNullException(nameof(paths)); ;
            }
            var oldCd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(pwd);
            // need to see if the path points to a file or directory, and also need to account for globbing patterns
            foreach (var path in paths) {
                if (IsPathADirectory(path)) {
                    // validate as a directory path
                    if (DoesPathUseGlobbing(path)) {
                        var foundDirs = Directory.GetDirectories(path);
                        if (!foundDirs.Any()) {
                            _reporter.WriteVerboseLine($"Missing directory, no match for path with glob: path='{path}'.");
                            missingPaths.Add(path);
                        }
                        else {
                            // TODO: Probably should hide this behind a DEBUG build or some type of setting
                            // this could cause a lot of extra verbose output
                            _reporter.WriteVerboseLine($"validated path in sources, path='{path}'");
                        }
                    }
                    else {
                        var fullpath = Path.Combine(pwd, path);
                        if (!Directory.Exists(fullpath)) {
                            _reporter.WriteVerboseLine($"Missing directory: path='{path}', fullpath='{fullpath}'");
                            missingPaths.Add(path);
                        }
                        else {
                            // TODO: Probably should hide this behind a DEBUG build or some type of setting
                            // this could cause a lot of extra verbose output
                            _reporter.WriteVerboseLine($"validated path in sources, path='{path}'");
                        }
                    }
                }
                else {
                    // validate as a file path
                    if (DoesPathUseGlobbing(path)) {
                        var foundFiles = Directory.GetFiles(path);

                        if (!foundFiles.Any()) {
                            _reporter.WriteVerboseLine($"Missing file, no match for path with glob: path='{path}'.");
                            missingPaths.Add(path);
                        }
                        else {
                            // TODO: Probably should hide this behind a DEBUG build or some type of setting
                            // this could cause a lot of extra verbose output
                            _reporter.WriteVerboseLine($"validated path in sources, path='{path}'");
                        }
                    }
                    else {
                        var fullpath = Path.Combine(pwd, path);
                        if (!File.Exists(fullpath)) {
                            _reporter.WriteVerboseLine($"Missing file: path='{path}', fullpath='{fullpath}'");
                            missingPaths.Add(path);
                        }
                        else {
                            // TODO: Probably should hide this behind a DEBUG build or some type of setting
                            // this could cause a lot of extra verbose output
                            _reporter.WriteVerboseLine($"validated path in sources, path='{path}'");
                        }
                    }
                }
            }
            Directory.SetCurrentDirectory(oldCd);

            return (missingPaths.Count == 0, missingPaths);
        }
        /// <summary>
        /// Directory paths should end with a slash, otherwise it should be a file path
        /// </summary>
        private bool IsPathADirectory(string path) {
            var trimmedPath = path.Trim();
            return trimmedPath.EndsWith("\\") || trimmedPath.EndsWith("/");
        }
        // for now assume that * is the only character for globbing, need to get more info here
        private bool DoesPathUseGlobbing(string path) {
            return path.Contains("*",StringComparison.OrdinalIgnoreCase);
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
            var sb = new StringBuilder();


            sb.Append(prefix);
            if (!string.IsNullOrEmpty(typeStr)) {
                sb.Append(typeStr);
                sb.Append(": ");
            }
            sb.Append(message);

            _reporter.WriteLine(sb.ToString());
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
        private bool HasFrameworkSymbolDefined(JToken template) {
            if(template == null) {
                throw new ArgumentNullException(nameof(template), "template cannot be null");
            }

            var queryResult = template.SelectToken("$.symbols.Framework");

            return queryResult != null ? true : false;
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

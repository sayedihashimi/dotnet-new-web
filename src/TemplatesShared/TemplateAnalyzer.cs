using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
namespace TemplatesShared {
    /// <summary>
    /// Checks:
    ///      1. template.json => validate with latest schema
    ///      2. Check for required, and recommended, properties in the template.json file
    ///      3. Analyze symbols for issues (todo: talk to Phil)
    ///      4. host file => validate with latest schema 
    ///      5. Check for ide.host.json (or similar filename) and for recommended properties
    ///      6. Analyze symbolInfo for issues (todo: talk to Phil)
    /// required properties of the template.json file
    ///  name, sourceName, tags:type==[project,item]
    /// recommended properties of the template.json file
    ///  $schema, defaultName, tags:language,  author, classifications (is it required?), Framework symbol
    /// recommended properties of the ide.host.json file
    ///  $schema, icon
    /// </summary>
    public class TemplateAnalyzer : ITemplateAnalyzer {

        public TemplateAnalyzer(IReporter reporter, IJsonSchemaHelper schemaHelper) {
            Debug.Assert(reporter != null);
            Debug.Assert(schemaHelper != null);
            _reporter = reporter;
            _schemaHelper = schemaHelper;
        }

        private IReporter _reporter;
        private IJsonSchemaHelper _schemaHelper;

        public void Analyze(string templateFolder) {
            Debug.Assert(!string.IsNullOrEmpty(templateFolder));
            _reporter.WriteLine($"Validating folder '{templateFolder}'");
            // validate the folder has a .template.config folder
            if (!Directory.Exists(templateFolder)) {
                _reporter.WriteLine($"ERROR: templateFolder not found at '{templateFolder}'", "    ");
                return;
            }

            var templateJsonFile = Path.Combine(templateFolder, ".template.config/template.json");
            if (!File.Exists(templateJsonFile)) {
                _reporter.WriteLine($"template.json not found at '{templateJsonFile}'", "    ");
                return;
            }

            var templateJsonSchemaUrl = @"https://json.schemastore.org/template";
            // templateJsonSchemaUrl = @"https://gist.githubusercontent.com/sayedihashimi/950195c4cfc4cdfee6f5184cf1ab000a/raw/d5ed30b3b3ab93a0c5d126fe33932d635651f705/template.schema.json";
            var schemaFile = _schemaHelper.GetSchemaFileFor(templateJsonSchemaUrl);
            
            var errors = _schemaHelper.Validate(schemaFile, templateJsonFile);
            if (errors == null || errors.Count == 0) {
                _reporter.WriteLine("    No schema issues found", "    ");
            }
            else {
                foreach (var vr in errors) {
                    _reporter.WriteLine(_schemaHelper.GetErrorStringFor(vr), "    ");
                }
            }
        }

        protected IList<ValidationError> ValidateUsingSchema(string schemaPath, string fileToValidate) {
            Debug.Assert(File.Exists(schemaPath));
            Debug.Assert(File.Exists(fileToValidate));

            _reporter.WriteLine($"Validating file: {fileToValidate}");
            var prefix = "    ";
            var vResult = _schemaHelper.Validate(schemaPath, fileToValidate);
            return vResult;

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

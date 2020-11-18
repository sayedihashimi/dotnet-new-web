using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace TemplatesShared {
    public interface IJsonHelper {
        string GetJsonFromFile(string filepath);
        string GetStringValue(JToken token);
        string GetStringValueFromQuery(JToken jobj, string jsonPath);
        bool HasValue(JToken token);
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

        public string GetJsonFromFile(string filepath) {
            JToken result = LoadJsonFrom(filepath);
            return result != null ? result.ToString() : null;
        }

        protected string NormalizeKey(string key) {
            if (string.IsNullOrEmpty(key)) { return key; }

            return string.IsNullOrEmpty(key) ? key : key.Trim().ToLowerInvariant();
        }

        public string GetStringValueFromQuery(JToken jobj, string jsonPath) {
            if(!HasValue(jobj) || string.IsNullOrEmpty(jsonPath)) {
                return null;
            }

            var queryResult = jobj.SelectToken(jsonPath);
            if(queryResult == null) {
                return null;
            }

            var jValue = queryResult as JValue;
            return jValue != null ? jValue.Value as string : (string)null;
        }
        public string GetStringValue(JToken token) {
            var jValue = token as JValue;
            return jValue != null ? jValue.Value as string : (string)null;
        }
        public bool HasValue(JToken token) {
            if (token == null) {
                return false;
            }

            var value = token as JValue;
            if (value != null) {
                if (value.Value == null) {
                    return false;
                }
                if (value.Value is string) {
                    if (string.IsNullOrEmpty((string)(value.Value))) {
                        return false;
                    }
                }
            }

            var array = token as JArray;
            if (array != null) {
                if (array.Count == 0) {
                    return false;
                }
            }

            return true;
        }
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

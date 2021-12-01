using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared {
    public class AnalyzeResult {
        public bool FoundIssues {
            get {
                return Issues != null && Issues.Any();
            }
        }
        public List<FoundIssue> Issues { get; set; } = new List<FoundIssue>();

        public bool HasErrors() => 
            Issues.Any(fi=>fi.IssueType == ErrorWarningType.Error);

        public static AnalyzeResult Combine(AnalyzeResult result1, AnalyzeResult result2) {
            var combinedResult = new AnalyzeResult();

            combinedResult.Issues.AddRange(result1.Issues);
            combinedResult.Issues.AddRange(result2.Issues);
            return combinedResult;
        }
    }
}

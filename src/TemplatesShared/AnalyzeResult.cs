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

        public static AnalyzeResult Combine(AnalyzeResult result1, AnalyzeResult result2) {
            var combinedResult = new AnalyzeResult();
            var allIssues = new List<FoundIssue>();

            allIssues.AddRange(result1.Issues);
            allIssues.AddRange(result2.Issues);
            combinedResult.Issues = allIssues;
            return combinedResult;
        }
    }
}

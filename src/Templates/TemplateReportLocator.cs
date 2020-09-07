using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Templates {
    public interface ITemplateReportLocator {
        string GetTemplateReportJsonPath();
    }

    public class TemplateReportLocator : ITemplateReportLocator {
        public string GetTemplateReportJsonPath() {
            var assemblyDir = new FileInfo(new System.Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName;
            var filepath = Path.Join(assemblyDir, "template-report.json");

            if (!File.Exists(filepath)) {
                throw new FileNotFoundException($"template report not found at '{filepath}'");
            }

            return filepath;
        }
    }
}

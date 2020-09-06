using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using TemplatesShared;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using System.Reflection.Metadata;

namespace TemplatesConsole {
    public class AnalyzeReportCommand : TemplateCommand {
        private HttpClient _httpClient = new HttpClient();
        private IRemoteFile _remoteFile;

        public AnalyzeReportCommand(IRemoteFile remoteFile) {
            Debug.Assert(remoteFile != null);
            this._remoteFile = remoteFile;

            Name = "analyze";
            Description = "will analyze the template-report.json file and return some info";
        }

        public override void Setup(CommandLineApplication command) {
            base.Setup(command);

            var optionTemplateReportJsonPath = command.Option<string>(
                "-trp|--templateReportPath",
                "the path to the template-report.json file",
                CommandOptionType.SingleValue);
            optionTemplateReportJsonPath.IsRequired();

            var optionAnalysisResultFilePath = command.Option<string>(
                "-arp|--analysisResultPath",
                "path to where the results will be written to",
                CommandOptionType.SingleValue);

            OnExecute = () => {
                EnableVerboseOption = OptionVerbose.HasValue();

                var templateReportJsonPath = optionTemplateReportJsonPath.Value();
                if (!File.Exists(templateReportJsonPath)) {
                    throw new FileNotFoundException($"template-report.json file not found at {templateReportJsonPath}");
                }

                var templatePacks = TemplatePack.CreateFromFile(templateReportJsonPath);
                
                // write the results to a temp file, and then copy to the final destination
                string resultsPath = optionAnalysisResultFilePath.HasValue() ? optionAnalysisResultFilePath.Value() : "template-pack-analysis.csv";
                CreateTemplatePackFile(templatePacks, templateReportJsonPath, resultsPath);

                // create the json file that contains all the templates                
                var allTemplates = new List<Template>();
                var allTemplateInfos = new List<TemplateReportSummaryInfo>();
                foreach (var tp in templatePacks) {
                    var extractFolderPath = Path.Combine(_remoteFile.CacheFolderpath, "extracted", ($"{tp.Package}.{tp.Version}.nupkg").ToLowerInvariant());
                    var templates = TemplatePack.GetTemplateFilesUnder(extractFolderPath);
                    foreach (var template in templates) {
                        var templateObj = Template.CreateFromFile(template);
                        allTemplates.Add(templateObj);
                        allTemplateInfos.Add(new TemplateReportSummaryInfo { Template = templateObj });
                    }
                }

                CreateAllTemplatesJsonFile(allTemplates, resultsPath);

                // create the template-details.csv file now
                CreateTemplateDetailsCsvFile(allTemplateInfos, resultsPath);

                return 1;
            };
        }

        private void CreateTemplatePackFile(List<TemplatePack> templatePacks,string templateReportJsonPath, string resultsPath) {
            var tempfilepath = Path.GetTempFileName();
            //var templatePacks = TemplatePack.CreateFromFile(templateReportJsonPath);
            using var writer = new StreamWriter(tempfilepath);
            writer.WriteLine("package name, version, has lib folder, packagetype");
            foreach (var tp in templatePacks) {
                var info = new TemplatePackReportInternalSummaryInfo(_remoteFile.CacheFolderpath, tp);
                var line = GetTemplatePackReportLineFor(info);
                Console.WriteLine(line);
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();

            // string resultsPath = optionAnalysisResultFilePath.HasValue() ? optionAnalysisResultFilePath.Value() : "template-pack-analysis.csv";
            Console.WriteLine($"writing analysis file to {resultsPath}");
            File.Copy(tempfilepath, resultsPath, true);

            // now create the template-details.csv file
            var allTemplates = new List<Template>();
            var allTemplateInfos = new List<TemplateReportSummaryInfo>();
            foreach (var tp in templatePacks) {
                var extractFolderPath = Path.Combine(_remoteFile.CacheFolderpath, "extracted", ($"{tp.Package}.{tp.Version}.nupkg").ToLowerInvariant());
                var templates = TemplatePack.GetTemplateFilesUnder(extractFolderPath);
                foreach (var template in templates) {
                    var templateObj = Template.CreateFromFile(template);
                    allTemplates.Add(templateObj);
                    allTemplateInfos.Add(new TemplateReportSummaryInfo { Template = templateObj });
                }
            }
        }

        private void CreateAllTemplatesJsonFile(List<Template> allTemplates, string resultsPath) {
            // write out the json file that contains all the templates
            var tempTemplateDetailsFilepath = Path.GetTempFileName();
            var allTemplatesJson = JsonConvert.SerializeObject(allTemplates, Formatting.Indented);
            File.WriteAllText(tempTemplateDetailsFilepath, allTemplatesJson);
            // file path is same as the results but name has -templates and extension is .json
            var templatesJsonFilePath = $"{resultsPath.Substring(0, resultsPath.Length - 4)}-templates.json";
            File.Copy(tempTemplateDetailsFilepath, templatesJsonFilePath, true);
        }

        private void CreateTemplateDetailsCsvFile(List<TemplateReportSummaryInfo> allTemplateInfos, string resultsPath) {
            var templateDetailsTempFilePath = Path.GetTempFileName();
            using var templateDetailsWriter = new StreamWriter(templateDetailsTempFilePath);
            templateDetailsWriter.WriteLine("name,template-type,author,sourceName,defaultName,baseline,language,primaryOutputs,tags,identity,groupIdentity");
            foreach (var templateInfo in allTemplateInfos) {
                templateDetailsWriter.WriteLine(GetTemplateDetailsReportLineFor(templateInfo));
            }
            templateDetailsWriter.Flush();
            templateDetailsWriter.Close();

            var templateDetailsDestPath = Path.Combine(Path.GetDirectoryName(resultsPath), "template-details.csv");
            File.Copy(templateDetailsTempFilePath, templateDetailsDestPath, true);
        }

        private string ReplaceComma(string str) {
            if (string.IsNullOrEmpty(str)) {
                return string.Empty;
            }

            return str.Replace(",", "|");
        }
        private string GetTemplatePackReportLineFor(TemplatePackReportInternalSummaryInfo info) {
            Debug.Assert(info != null);

            var line = $"{ReplaceComma(info.PackageName)},{ReplaceComma(info.Version)},{info.HasLibFolder}, {ReplaceComma(GetTemplatePackReportStringFor(info.PackageType))}";
            return line;
        }
        private string GetTemplatePackReportStringFor(IList<string> packageType, string delim = " ") {           
            if(packageType == null || packageType.Count <= 0) {
                return string.Empty;
            }

            return string.Join(delim, packageType);
        }
        private string GetTemplateDetailsReportLineFor(TemplateReportSummaryInfo templateInfo) {
            Debug.Assert(templateInfo != null);
            Debug.Assert(templateInfo.Template != null);

            var template = templateInfo.Template;
            var line = $"{ReplaceComma(template.Name)},{ReplaceComma(template.GetTemplateType())},{ReplaceComma(template.Author)},{ReplaceComma(template.SourceName)},{ReplaceComma(template.DefaultName)},{ReplaceComma(template.Baseline)},{ReplaceComma(template.GetLanguage())},{ReplaceComma(GetTemplateDetailsReportStringFor(template.PrimaryOutputs))},{ReplaceComma(GetTemplateDetailsStringForTags(template.Tags))},{ReplaceComma(template.Identity)},{ReplaceComma(template.GroupIdentity)}";

            return line;
        }
        private string GetTemplateDetailsReportStringFor(PrimaryOutput[]primaryOutputs) {
            if(primaryOutputs == null) {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var po in primaryOutputs) {
                sb.Append($"'{po.Path}';");
            }

            return sb.ToString();
        }
        private string GetTemplateDetailsStringForTags(Dictionary<string,string>tags) {
            if(tags == null || tags.Keys.Count <= 0) {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach(var key in tags.Keys) {
                sb.Append($"'{key}':'{tags[key]}'");
            }

            return sb.ToString();
        }
    }
    public class TemplatePackReportInternalSummaryInfo {
        private string _cacheFolderPath;
        public TemplatePackReportInternalSummaryInfo() : this(null,null) { 
        }
        public TemplatePackReportInternalSummaryInfo(string cacheFolderPath) : this(cacheFolderPath, null) {
        }
        public TemplatePackReportInternalSummaryInfo(string cacheFolderPath, TemplatePack tp) {
            _cacheFolderPath = cacheFolderPath;
            InitFrom(tp);
        }
        public string PackageName { get; set; }
        public string Version { get; set; }
        public List<string> PackageType { get; set; }
        public bool HasLibFolder { get; set; }

        private void InitFrom(TemplatePack tp) {
            Debug.Assert(tp != null);

            var extractFolderPath = Path.Combine(_cacheFolderPath, "extracted", Normalize($"{tp.Package}.{tp.Version}.nupkg"));
            if (!Directory.Exists(extractFolderPath)) {
                throw new DirectoryNotFoundException($"directory not found at {extractFolderPath}");
            }

            PackageName = tp.Package;
            Version = tp.Version;
            HasLibFolder = Directory.Exists(Path.Combine(extractFolderPath, "lib"));

            // read the nuspec file to get the package type
            var nuspecFilePath = Path.Combine(extractFolderPath, Normalize($"{tp.Package}.nuspec"));
            if (!File.Exists(nuspecFilePath)) {
                throw new FileNotFoundException($"nuspec file not found at {nuspecFilePath}");
            }

            XDocument xmldoc = XDocument.Parse(File.ReadAllText(nuspecFilePath));

            var f = xmldoc.Root.Descendants("package");

            var result = xmldoc.Root.Attributes().
                Where(a => a.IsNamespaceDeclaration && string.Compare("xmlns", a.Name.LocalName) == 0).
                First().Value;

            var packageType = (from e in xmldoc.Descendants()
                       where e.Name.Namespace == result
                       where e.Name.LocalName == "packageType"
                       select e.Attribute("name").Value).ToList();
            packageType.Sort();
            PackageType = packageType;
        }
        private string Normalize(string keyStr) {
            return keyStr.ToLowerInvariant();
        }
    }

    public class TemplateReportSummaryInfo {
        public Template Template { get; set; }

        private void InitFrom(string templateFilepath) {
            Debug.Assert(File.Exists(templateFilepath));

        }

    }
}

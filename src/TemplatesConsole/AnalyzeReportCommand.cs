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

            //var optionAnalysisResultFilePath = command.Option<string>(
            //    "-arp|--analysisResultPath",
            //    "path to where the results will be written to",
            //    CommandOptionType.SingleValue);

            var optionOutputDir = command.Option<string>(
                "-od|--output-dir",
                "folder path where files will be written",
                CommandOptionType.SingleValue);

            OnExecute = () => {
                EnableVerboseOption = OptionVerbose.HasValue();

                var templateReportJsonPath = optionTemplateReportJsonPath.Value();
                if (!File.Exists(templateReportJsonPath)) {
                    throw new FileNotFoundException($"template-report.json file not found at {templateReportJsonPath}");
                }

                var templatePacks = TemplatePack.CreateFromFile(templateReportJsonPath);
                List<string> createdFiles = new List<string>();

                string outdir = optionOutputDir.HasValue() ? optionOutputDir.Value() : Directory.GetCurrentDirectory();

                string templatePackFile = Path.Combine(outdir, "template-pack-analysis.csv");
                CreateTemplatePackFile(templatePacks, templateReportJsonPath, templatePackFile);
                createdFiles.Add(templatePackFile);

                // create the json file that contains all the templates                
                var allTemplates = new List<Template>();
                var allTemplateInfos = new List<TemplateReportSummaryInfo>();
                var allHostFiles = new List<TemplateHostFile>();
                foreach (var tp in templatePacks) {
                    var extractFolderPath = Path.Combine(_remoteFile.CacheFolderpath, "extracted", ($"{tp.Package}.{tp.Version}.nupkg").ToLowerInvariant());

                    // populate the HostFiles property of the template pack
                    var templates = TemplatePack.GetTemplateFilesUnder(extractFolderPath);
                    foreach (var template in templates) {
                        var templateObj = Template.CreateFromFile(template);
                        templateObj.TemplatePackId = tp.Package;
                        templateObj.InitHostFilesFrom(Path.GetDirectoryName(template), templateObj.TemplatePackId, templateObj.Name);
                        
                        allTemplates.Add(templateObj);
                        allTemplateInfos.Add(new TemplateReportSummaryInfo { Template = templateObj });
                        if(templateObj.HostFiles != null && templateObj.HostFiles.Count > 0) {
                            allHostFiles.AddRange(templateObj.HostFiles);
                        }
                    }
                }

                var allTemplatesJsonPath = Path.Combine(outdir, "template-all.json");
                CreateAllTemplatesJsonFile(allTemplates, allTemplatesJsonPath);
                createdFiles.Add(allTemplatesJsonPath);

                // create the template-details.csv file now
                var templateDetailsCsvPath = Path.Combine(outdir, "template-details.csv");
                CreateTemplateDetailsCsvFile(allTemplateInfos, templateDetailsCsvPath);
                createdFiles.Add(templateDetailsCsvPath);

                var hostFileDetailsCsvPath = Path.Combine(outdir, "template-host-files.csv");
                CreateHostFilesDetailsCsvFile(allHostFiles, hostFileDetailsCsvPath);
                createdFiles.Add(hostFileDetailsCsvPath);

                Console.WriteLine("Created files:");
                foreach(var cf in createdFiles)
                {
                    Console.WriteLine($"    {cf}");
                }

                return 1;
            };
        }

        private void CreateTemplatePackFile(List<TemplatePack> templatePacks,string templateReportJsonPath, string resultsPath) {
            // write the results to a temp file, and then copy to the final destination
            var tempfilepath = Path.GetTempFileName();
            //var templatePacks = TemplatePack.CreateFromFile(templateReportJsonPath);
            using var writer = new StreamWriter(tempfilepath);
            writer.WriteLine("package name, version, has lib folder, packagetype,num-downloads");

            // list of template packs that could not be initalized for some reason
            // they need to be removed from the list to prevent futher issues
            var tpToRemove = new List<TemplatePack>();
            foreach (var tp in templatePacks) {
                TemplatePackReportInternalSummaryInfo info = null;
                try {
                    info = new TemplatePackReportInternalSummaryInfo(_remoteFile.CacheFolderpath, tp);
                }
                catch(TemplateInitException tie) {
                    // TODO: Reporter should be used instead of writing directly to the console
                    Console.WriteLine($"ERROR: Unable to initalize template pack from '{tp.Package}', skipping this one. Error: {tie.ToString()}");
                    tpToRemove.Add(tp);
                    continue;
                }
                var line = GetTemplatePackReportLineFor(info);
                Console.WriteLine(line);
                writer.WriteLine(line);
            }
            writer.Flush();
            writer.Close();

            if(tpToRemove.Count > 0) {
                foreach(var tp in tpToRemove) {
                    templatePacks.Remove(tp);
                }
            }

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
            File.Copy(tempTemplateDetailsFilepath, resultsPath, true);
        }

        private void CreateTemplateDetailsCsvFile(List<TemplateReportSummaryInfo> allTemplateInfos, string resultsPath) {
            var templateDetailsTempFilePath = Path.GetTempFileName();
            using var templateDetailsWriter = new StreamWriter(templateDetailsTempFilePath);
            templateDetailsWriter.WriteLine("name,template-pack-id,template-type,author,sourceName,defaultName,baseline,language,primaryOutputs,tags,identity,groupIdentity,host files,local-file-path,num-symbols,num-parameters,classifications");
            foreach (var templateInfo in allTemplateInfos) {
                templateDetailsWriter.WriteLine(GetTemplateDetailsReportLineFor(templateInfo));
            }
            templateDetailsWriter.Flush();
            templateDetailsWriter.Close();

            var templateDetailsDestPath = Path.Combine(Path.GetDirectoryName(resultsPath), "template-details.csv");
            File.Copy(templateDetailsTempFilePath, resultsPath, true);
        }
        private void CreateHostFilesDetailsCsvFile(List<TemplateHostFile>hostFiles, string resultsPath)
        {
            if (string.IsNullOrWhiteSpace(resultsPath)) {
                throw new ArgumentNullException("Cannot create host file because resultsPath is empty");
            }
            if(hostFiles == null || hostFiles.Count <= 0) {
                Console.WriteLine("skipping host file csv creation because host file list is empty");
                return;
            }

            var hfTempfilepath = Path.GetTempFileName();
            using var hfWriter = new StreamWriter(hfTempfilepath);
            hfWriter.WriteLine("template-pack-id,template-name,icon,learn-more-link,ui-filters,minfullframeworkversion,local-file-path,template-local-file-path");
            foreach(var hf in hostFiles) {
                if(hf == null) {
                    continue;
                }
                hfWriter.WriteLine(GetHostFilesLineFor(hf));
            }
            hfWriter.Flush();
            hfWriter.Close();

            File.Copy(hfTempfilepath, resultsPath, true);
        }
        

        private string ReplaceComma(string str) {
            if (string.IsNullOrEmpty(str)) {
                return string.Empty;
            }

            return str.Replace(",", "|");
        }
        private string GetTemplatePackReportLineFor(TemplatePackReportInternalSummaryInfo info) {
            Debug.Assert(info != null);

            var line = $"{ReplaceComma(info.PackageName)},{ReplaceComma(info.Version)},{info.HasLibFolder}, {ReplaceComma(GetTemplatePackReportStringFor(info.PackageType))},{info.DownloadCount}";
            return line;
        }
        private string GetTemplatePackReportStringFor(IList<string> packageType, string delim = " ") {           
            if(packageType == null || packageType.Count <= 0) {
                return string.Empty;
            }

            return string.Join(delim, packageType);
        }
        private string GetTemplatePackReportStringForHostFiles(Template info)
        {
            if(info == null || info.HostFiles == null || info.HostFiles.Count <= 0)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();

            for(var i = 0; i<info.HostFiles.Count; i++)
            {
                string filepath = info.HostFiles[i].LocalFilePath;
                if (string.IsNullOrWhiteSpace(filepath)){
                    continue;
                }
                sb.Append(Path.GetFileName(filepath));

                if(i < info.HostFiles.Count -1)
                {
                    sb.Append(";");
                }
            }

            return sb.ToString();

        }
        private string GetTemplateDetailsReportLineFor(TemplateReportSummaryInfo templateInfo) {
            Debug.Assert(templateInfo != null);
            Debug.Assert(templateInfo.Template != null);

            var template = templateInfo.Template;
            if(template.Symbols == null) { template.Symbols = new List<TemplateSymbolInfo>(); }
            
            var parameters = GetParametersFrom(template.Symbols);
            if(parameters == null){ parameters = new List<TemplateSymbolInfo>();}
            
            var sb = new StringBuilder();
            sb.Append(ReplaceComma(template.Name));
            sb.Append(",");
            sb.Append(ReplaceComma(template.TemplatePackId));
            sb.Append(",");
            sb.Append(ReplaceComma(template.GetTemplateType()));
            sb.Append(",");
            sb.Append(ReplaceComma(template.Author));
            sb.Append(",");
            sb.Append(ReplaceComma(template.SourceName));
            sb.Append(",");
            sb.Append(ReplaceComma(template.DefaultName));
            sb.Append(",");
            sb.Append(ReplaceComma(template.Baseline));
            sb.Append(",");
            sb.Append(ReplaceComma(template.GetLanguage()));
            sb.Append(",");
            sb.Append(ReplaceComma(GetTemplateDetailsReportStringFor(template.PrimaryOutputs)));
            sb.Append(",");
            sb.Append(ReplaceComma(GetTemplateDetailsStringForTags(template.Tags)));
            sb.Append(",");
            sb.Append(ReplaceComma(template.Identity));
            sb.Append(",");
            sb.Append(ReplaceComma(template.GroupIdentity));
            sb.Append(",");
            sb.Append(ReplaceComma(GetTemplatePackReportStringForHostFiles(template)));
            sb.Append(",");
            sb.Append(ReplaceComma(template.LocalFilePath));
            sb.Append(",");
            sb.Append(template.Symbols.Count);
            sb.Append(",");
            sb.Append(parameters.Count);
            sb.Append(",");
            sb.Append(ReplaceComma(GetTemplateDetailsClassificationsStringFor(template.Classifications)));

            return sb.ToString();
        }

        private string GetHostFilesLineFor(TemplateHostFile hostFile)
        {
            if(hostFile == null)
            {
                return string.Empty;
            }
            var sb = new StringBuilder();
            // template-pack-id,template-name,icon,learn-more-link,ui-filters,minfullframeworkversion,local-file-path
            sb.Append(ReplaceComma(hostFile.TempaltePackId));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.TemplateName));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.Icon));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.LearnMoreLink));
            sb.Append(",");
            sb.Append(ReplaceComma(string.Join(';', hostFile.UiFilters)));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.MinFullFrameworkVersion));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.LocalFilePath));
            sb.Append(",");
            sb.Append(ReplaceComma(hostFile.TemplateLocalFilePath));

            return sb.ToString();
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
        private string GetTemplateDetailsClassificationsStringFor(string[]classifications) {
            var sb = new StringBuilder();

            if(classifications != null && classifications.Length > 0) {
                for(var i = 0; i < classifications.Length; i++) {
                    var c = classifications[i];
                    if (string.IsNullOrEmpty(c)) {
                        continue;
                    }
                    c = c.ToLowerInvariant();
                    sb.Append(classifications[i]);
                    if(i<classifications.Length - 1) {
                        sb.Append(";");
                    }
                }
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
        private List<TemplateSymbolInfo> GetParametersFrom(List<TemplateSymbolInfo>symbols) {
            var result = new List<TemplateSymbolInfo>();

            if(symbols == null || symbols.Count == 0) {
                return result;
            }

            foreach(var symbol in symbols) {
                if(symbol == null || string.IsNullOrEmpty(symbol.Type)) {
                    continue;
                }
                if(string.Compare("parameter",symbol.Type,StringComparison.OrdinalIgnoreCase) == 0) {
                    result.Add(symbol);
                }
            }

            return result;
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
        public List<string> HostFiles { get; set; }
        public int DownloadCount { get; set; }

        private void InitFrom(TemplatePack tp) {
            Debug.Assert(tp != null);

            var extractFolderPath = Path.Combine(_cacheFolderPath, "extracted", Normalize($"{tp.Package}.{tp.Version}.nupkg"));
            if (!Directory.Exists(extractFolderPath)) {
                throw new TemplateInitException($"directory not found at {extractFolderPath}");
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
            DownloadCount = tp.DownloadCount;
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

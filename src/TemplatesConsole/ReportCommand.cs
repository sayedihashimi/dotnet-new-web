using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using TemplatesShared;

namespace TemplatesConsole {
    public class ReportCommand : TemplateCommand {
        protected HttpClient _httpClient = new HttpClient();
        private INuGetHelper _nugetHelper;
        private IRemoteFile _remoteFile;
        private INuGetPackageDownloader _nugetPkgDownloader;
        private TemplatesShared.IReporter _reporter;
        public ReportCommand(HttpClient httpClient, INuGetHelper nugetHelper, IRemoteFile remoteFile, INuGetPackageDownloader nugetPkgDownloader, TemplatesShared.IReporter reporter): base() {
            Debug.Assert(httpClient != null);
            Debug.Assert(nugetHelper != null);
            Debug.Assert(remoteFile != null);
            Debug.Assert(nugetPkgDownloader != null);

            _httpClient = httpClient;
            _nugetHelper = nugetHelper;
            _remoteFile = remoteFile;
            _nugetPkgDownloader = nugetPkgDownloader;
            _reporter = reporter;
            Name = "report";
            Description = "will create the template report into a json file";
        }

        public override void Setup(CommandLineApplication command) {
            base.Setup(command);

            DateTime startTime = DateTime.Now;
            Console.WriteLine("Starting at {0}", startTime.ToString("MM.dd.yy-H.m.s.ffff"));
            var optionReportJsonPath = command.Option<string>(
                "-rp|--jsonReportPath",
                "path to where the json report should be written",
                CommandOptionType.SingleValue);
            
            var optionCacheFolderPath = command.Option<string>(
                "-cf|--cacheFolderPath",
                $"directory path to where the local cache will be. Default path: '{_remoteFile.CacheFolderpath}'",
                CommandOptionType.SingleValue);

            //default: 'template','templates', 'ServiceStack.Core.Templates', 'BlackFox.DotnetNew.FSharpTemplates','libyear','libyear',
            //'angular-cli.dotnet','Carna.ProjectTemplates','SerialSeb.Templates.ClassLibrary','Pioneer.Console.Boilerplate'
            var optionSearchTerms = command.Option<string>(
                "-st|--searchTerm",
                "term to search on nuget. This option may be provided multiple times. If not provided the default set of values will be used.",
                CommandOptionType.MultipleValue);

            var optionPreviousReportPath = command.Option<string>(
                "-lr|--lastReport",
                "path to the last template-report.json file",
                CommandOptionType.SingleValue);

            var optionSpecificPackagesToInclude = command.Option<string>(
                "-p|--packageToInclude",
                "list of specific packages that should be included.",
                CommandOptionType.MultipleValue);

            OnExecute = () => {
                EnableVerboseOption = OptionVerbose.HasValue();

                var report = new TemplateReport(_nugetHelper, _httpClient, _nugetPkgDownloader, _remoteFile, _reporter);

                var searchTerms = optionSearchTerms.HasValue() ? optionSearchTerms.Values.ToArray() : GetDefaultSearchTerms();

                var templateReportPath = optionReportJsonPath.HasValue() ? 
                                        optionReportJsonPath.Value() : 
                                        Path.Combine(Directory.GetCurrentDirectory(), "template-report.json");

                var specificPackages = new List<string>();
                if (optionSpecificPackagesToInclude.HasValue()) {
                    specificPackages.AddRange(optionSpecificPackagesToInclude.Values.ToArray());
                }

                string previousReportPath = optionPreviousReportPath.HasValue() ? optionPreviousReportPath.Value() : null;

                report.GenerateTemplateJsonReportAsync(searchTerms, templateReportPath, specificPackages, previousReportPath).Wait();

                DateTime finishTime = DateTime.Now;
                TimeSpan timespent = finishTime.Subtract(startTime);
                Console.WriteLine("Finished at {0}\nTime taken (sec):{1}", finishTime.ToString("MM.dd.yy-H.m.s.ffff"), timespent.TotalSeconds);

                return 1;
            };
        }

        private string[] GetDefaultSearchTerms() {
            var str = "template,templates, ServiceStack.Core.Templates, BlackFox.DotnetNew.FSharpTemplates,libyear,libyear,angular-cli.dotnet,Carna.ProjectTemplates,SerialSeb.Templates.ClassLibrary,Pioneer.Console.Boilerplate";
            return str.Split(',');
        }
    }
}

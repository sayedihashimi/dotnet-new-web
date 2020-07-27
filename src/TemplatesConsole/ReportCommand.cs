using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using TemplatesShared;

namespace TemplatesConsole {
    public class ReportCommand : TemplateCommand {
        protected HttpClient _httpClient = new HttpClient();
        private INuGetHelper _nugetHelper;
        private IRemoteFile _remoteFile;
        public ReportCommand(HttpClient httpClient, INuGetHelper nugetHelper, IRemoteFile remoteFile): base() {
            Debug.Assert(httpClient != null);
            Debug.Assert(nugetHelper != null);
            Debug.Assert(remoteFile != null);

            _httpClient = httpClient;
            _nugetHelper = nugetHelper;
            _remoteFile = remoteFile;
        }

        public override void Setup(CommandLineApplication command) {
            base.Setup(command);

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
            optionSearchTerms.IsRequired(allowEmptyStrings: false, errorMessage: "you must specify a search term with -st|--searchTerm");

            OnExecute = () => {
                var verbose = OptionVerbose.HasValue();
                
 


                return 1;
            };
        }

        private string[] GetDefaultSearchTerms() {
            var str = "template,templates, ServiceStack.Core.Templates, BlackFox.DotnetNew.FSharpTemplates,libyear,libyear,angular-cli.dotnet,Carna.ProjectTemplates,SerialSeb.Templates.ClassLibrary,Pioneer.Console.Boilerplate";
            return str.Split(',');
        }
    }
}

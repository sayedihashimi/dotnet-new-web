using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using TemplatesShared;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace TemplatesConsole {
    public class QueryCommand : TemplateCommand {
        protected HttpClient _httpClient = new HttpClient();
        private INuGetHelper _nugetHelper;
        public QueryCommand(INuGetHelper nugetHelper) : base() {
            Debug.Assert(nugetHelper != null);

            _nugetHelper = nugetHelper;
            Name = Strings.QueryCommandName;
            Description = Strings.QueryCommandDesc;
        }

        public override void Setup(CommandLineApplication command) {
            base.Setup(command);

            var optionSearchTerms = command.Option<string>(
                "-st|--searchTerm",
                "term to search on nuget. This option may be provided multiple times.",
                CommandOptionType.MultipleValue);
            optionSearchTerms.IsRequired(allowEmptyStrings: false, errorMessage: "you must specify a search term with -st|--searchTerm");

            var optionSaveFilePath = command.Option<string>(
                "-f|--savefilepath",
                "filepath where the results will be stored",
                CommandOptionType.SingleValue);

            var optionNoOutput = command.Option<string>(
                "--no-output",
                "when passed the results will not be displayed on the console. This is typically used with the -f|--savefilepath option.",
                CommandOptionType.NoValue);

            OnExecute = () => {
                var verbose = OptionVerbose.HasValue();
                var searchTerms = optionSearchTerms.ParsedValues.ToArray<string>();
                string filepath = optionSaveFilePath.HasValue() ? optionSaveFilePath.Value() : null;
                bool printResults = optionNoOutput.HasValue() ? true : false;

                void writeVerbose(string str) {
                    if (verbose) {
                        Console.WriteLine(str);
                    }
                }
                void writeOutput(string str) {
                    if (printResults) {
                        Console.WriteLine(str);
                    }
                }

                var found = _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, null).Result;

                writeOutput($"Num packages found: {found.Count}");

                if (!string.IsNullOrEmpty(filepath)) {
                    writeVerbose($"saving to filepath: '{filepath}'");
                    // convert pkg list to a json string
                    var jsonStr = JsonConvert.SerializeObject(found);
                    // write to a temp file and then copy to the final destination
                    var tempfilepath = Path.GetTempFileName();
                    if (File.Exists(tempfilepath)) { 
                        File.Delete(tempfilepath);
                    }
                    writeVerbose($"saving to tempfile at: '{tempfilepath}'");
                    File.WriteAllText(tempfilepath, jsonStr);
                    writeVerbose($"moving tempfile to destination '{tempfilepath}'->'{filepath}'");
                    File.Move(tempfilepath, filepath, true);
                }

                foreach(var pkg in found) {
                    writeOutput(pkg.ToString());
                }
                return 1;
            };
        }
    }
}

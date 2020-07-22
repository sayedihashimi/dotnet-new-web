using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using TemplatesShared;
using System.Linq;

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

            var optionQuery = command.Option<string>(
                "-st|--searchTerm",
                "term to search on nuget. This option may be provided multiple times.",
                CommandOptionType.MultipleValue);

            OnExecute = () => {
                var searchTerms = optionQuery.ParsedValues.ToArray<string>();
                var found = _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, null).Result;

                Console.WriteLine($"Num packages found: {found.Count}");

                foreach(var pkg in found) {
                    Console.WriteLine(pkg.ToString());
                }
                return 1;
//                var found = _nugetHelper.QueryNuGetAsync()
            };
        }

    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using TemplatesShared;

namespace Templates {
    public class SearchCommand : CommandBase {
        private readonly IReporter _reporter;
        private readonly ITemplateReportLocator _reportLocator;
        private readonly ITemplateSearcher _searcher;

        public SearchCommand(IReporter reporter, ITemplateReportLocator reportLocator, ITemplateSearcher searcher) {
            Debug.Assert(reporter != null);
            Debug.Assert(reportLocator != null);

            _reporter = reporter;
            _reportLocator = reportLocator;
            _searcher = searcher;
        }
        public override Command CreateCommand() =>
            new Command(name: "search", description: "search for templates") {
                CommandHandler.Create<string>( (searchTerm) => {
                    // load up the file
                    var templatePacks = TemplatePack.CreateFromFile(_reportLocator.GetTemplateReportJsonPath());
                    var result = _searcher.Search(searchTerm, templatePacks);
                    WriteResults(result);
                }),
                ArgSearchTerm()
            };

        protected Argument ArgSearchTerm() =>
            new Argument<string>(
                name: "search-term") {
                Description = "search term"
            };

        protected void WriteResults(IList<Template> templates) {
            if(templates == null || templates.Count <= 0) {
                _reporter.WriteLine("no matches found");
                return;
            }
            _reporter.WriteLine($"num results: {templates.Count}");
            foreach(var pack in templates) {
                _reporter.WriteLine($"{pack.Name}");
            }
        }
    }
}

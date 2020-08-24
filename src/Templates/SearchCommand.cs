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

                    DoPromptDemo();
                    return;
                    // load up the file
                    var templatePacks = TemplatePack.CreateFromFile(_reportLocator.GetTemplateReportJsonPath());
                    var result = _searcher.Search(searchTerm, templatePacks);
                    WriteResults(result);
                }),
                ArgSearchTerm()
            };

        private void DoPromptDemo() {
            var consoleWrapper = new DirectConsoleWrapper();

            var prompts = new List<Prompt> {
                new TrueFalsePrompt("Do you agree?"),
                new FreeTextPrompt("What is your name?"),
                new TrueFalsePrompt("Are you over 18 years old?"),
                new FreeTextPrompt("What is your SSN?")
            };

            //Prompt p1 = new TrueFalsePrompt("Do you agree?");
            //Prompt p2 = new FreeTextPrompt("What is your name?");
            //Prompt p3 = new TrueFalsePrompt("Are you over 18 years old?");
            //Prompt p4 = new FreeTextPrompt("What is your SSN?");
            PromptInvoker pi = new PromptInvoker(consoleWrapper);
            var promptResult = pi.GetPromptResults(prompts);

            Console.WriteLine(promptResult);
        }

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

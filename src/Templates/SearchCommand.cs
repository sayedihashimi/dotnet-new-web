using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
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
                    DoPromptDemo(true);
                    var templatePacks = TemplatePack.CreateFromFile(_reportLocator.GetTemplateReportJsonPath());
                    var result = _searcher.Search(searchTerm, templatePacks);

                    List<UserOption>options = new List<UserOption>();
                    foreach(var template in result) {
                        options.Add(new UserOption($"{template.Name} - ({template.TemplatePackId})"));
                    }

                    var consoleWrapper = new DirectConsoleWrapper();
                    PickManyPrompt pmp = new PickManyPrompt("Select templates to install",options);

                    bool doSharprompt = true;
                    IPromptInvoker pi;
                    pi = doSharprompt ? (IPromptInvoker)new SharPromptInvoker() : new PromptInvoker(consoleWrapper);

                    var promptResult = pi.GetPromptResult(pmp);
                    Console.WriteLine();
                    Console.WriteLine("Selection:");
                    PrintResults(new List<Prompt>{promptResult });
                }),
                ArgSearchTerm()
            };

        private void PrintResults(List<Prompt>prompts) {
            foreach (var pr in prompts) {
                OptionsPrompt optionsPrompt = pr as OptionsPrompt;
                if (optionsPrompt != null) {
                    Console.WriteLine($"  {pr.Text}:");
                    foreach (var uo in optionsPrompt.UserOptions) {
                        if (uo.IsSelected) {
                            Console.WriteLine($"    {uo.Text} => {uo.IsSelected}");
                        }
                    }
                }
                else {
                    Console.WriteLine($"  {pr.Text} => {pr.Result}");
                }
            }
        }
        private void DoPromptDemo(bool useSharprompt) {
            var consoleWrapper = new DirectConsoleWrapper();

            var prompts = new List<Prompt> {
                new FreeTextPrompt("What time is it?"),
                new TrueFalsePrompt("Do you agree?"),
                new PickOnePrompt ("Pick an option", UserOption.ConvertToOptions(new List<string> {
                    "option 1",
                    "option 2",
                    "option 3",
                    "option 4",
                    "option 5",
                })),
                new PickManyPrompt ("Pick one or more options", UserOption.ConvertToOptions(new List<string> {
                    "option 1",
                    "option 2",
                    "option 3",
                    "option 4",
                    "option 5",
                })),
            };

            IPromptInvoker pi;

            pi = useSharprompt ? (IPromptInvoker)new SharPromptInvoker() : new PromptInvoker(consoleWrapper);

            var promptResult = pi.GetPromptResult(prompts);

            Console.WriteLine();
            Console.WriteLine("Answers:");
            PrintResults(promptResult);
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
            foreach(var template in templates) {
                _reporter.WriteLine($"{template.Name}");
            }
        }
    }
}

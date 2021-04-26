using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TemplatesShared;

namespace Templates {
    public class SearchCommand : CommandBase {
        private readonly IReporter _reporter;
        private readonly ITemplateReportLocator _reportLocator;
        private readonly ITemplateSearcher _searcher;
        private readonly ITemplateInstaller _installer;
        public SearchCommand(IReporter reporter, ITemplateReportLocator reportLocator, ITemplateSearcher searcher, ITemplateInstaller installer) {
            Debug.Assert(reporter != null);
            Debug.Assert(reportLocator != null);
            Debug.Assert(searcher != null);
            Debug.Assert(installer != null);

            _reporter = reporter;
            _reportLocator = reportLocator;
            _searcher = searcher;
            _installer = installer;
        }
        public override Command CreateCommand() =>
            new Command(name: "search", description: "search for templates") {
                CommandHandler.Create<string>(async (searchTerm) => {
                    var previousTemplateReportPath = _reportLocator.GetTemplateReportJsonPath();
                    _reporter.WriteVerboseLine($"loading previous template-report.json from '{previousTemplateReportPath}'");
                    var templatePacks = TemplatePack.CreateFromFile(previousTemplateReportPath);
                    _reporter.WriteVerboseLine($"Num template packs in previous report: '{templatePacks.Count}'");
                    var result = _searcher.Search(searchTerm, templatePacks);

                    List<UserOption>options = new List<UserOption>();
                    foreach(var template in result) {
                        options.Add(new UserOption($"{template.Name} - ({template.TemplatePackId})", template));
                    }

                    var consoleWrapper = new DirectConsoleWrapper();
                    PickManyPrompt pmp = new PickManyPrompt("Select templates to install (↑↓ to navigate, Space to select, and Enter to commit)",options);
                    
                    bool doSharprompt = true;
                    IPromptInvoker pi;
                    pi = doSharprompt ? (IPromptInvoker)new SharPromptInvoker() : new PromptInvoker(consoleWrapper);

                    var promptResult = pi.GetPromptResult(pmp) as PickManyPrompt;

                    var templatesToInstall = promptResult.UserOptions
                                                            .Where(uo=>uo.IsSelected)
                                                            .Select(uo=>uo.Value as Template);
                    if(templatesToInstall == null) {
                        _reporter.WriteLine("noting selected to install");
                        return;
                    }

                    var templateInstallList = templatesToInstall.ToList();
                    await InstallTemplatesAsync(templateInstallList);
                }),
                ArgSearchTerm()
            };

        private async Task InstallTemplatesAsync(List<Template> templates) {
            Debug.Assert(templates != null && templates.Count > 0);

            var templatePackIds = templates.Select(t => t.TemplatePackId).Distinct().ToList();

            _reporter.WriteLine($"Istalling templates: {templatePackIds}");

            foreach(var tpId in templatePackIds) {
                await _installer.InstallPackageAsync(tpId);
            }
        }

        private void PrintResults(List<Prompt> prompts) {
            foreach (var pr in prompts) {
                OptionsPrompt optionsPrompt = pr as OptionsPrompt;
                if (optionsPrompt != null) {
                    Console.WriteLine($"  {pr.Text}:");
                    foreach (var uo in optionsPrompt.UserOptions) {
                        if (uo.IsSelected) {
                            Console.WriteLine($"    {uo.Text}[{((Template)uo.Value).TemplatePackId}] => {uo.IsSelected}");
                        }
                    }
                }
                else {
                    Console.WriteLine($"  {pr.Text} => {pr.Result}");
                }
            }
        }

        protected Argument ArgSearchTerm() =>
            new Argument<string>(
                name: "search-term") {
                Description = "search term"
            };

        protected void WriteResults(IList<Template> templates) {
            if (templates == null || templates.Count <= 0) {
                _reporter.WriteLine("no matches found");
                return;
            }
            _reporter.WriteLine($"num results: {templates.Count}");
            foreach (var template in templates) {
                _reporter.WriteLine($"{template.Name}");
            }
        }
    }
}

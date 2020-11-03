using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text;
using TemplatesShared;
using System.Linq;
using System.IO;

namespace Templates {
    /// <summary>
    /// Analyzes .net core templates
    /// </summary>
    public class AnalyzeTemplateCommand : CommandBase {
        private IReporter _reporter;
        private ITemplateAnalyzer _templateAnalyzer;

        public AnalyzeTemplateCommand(IReporter reporter, ITemplateAnalyzer analyzer) {
            Debug.Assert(reporter != null);
            Debug.Assert(analyzer != null);

            _reporter = reporter;
            _templateAnalyzer = analyzer;
        }

        // This will search for .nupkg files, identify the packages which are templates
        // and analyze them. If the path points to a .nupkg file, that will be analyzed
        // and results returned.
        // > templates analyze --packages --path c:\data\mycode\sample\templates

        // This will search for templates under the path specified
        // and analyze them.
        // > templates analyze --folders --path c:\data\mycode\sample\templates

        public override Command CreateCommand() =>
            new Command(name: "analyze", description: "template analyzer tool") {

                CommandHandler.Create<string[],string[]>(
                    (packages, folders) => {
                        _reporter.WriteLine("analyze command");

                        _reporter.WriteLine(packages != null ?
                            $"packages: {string.Join(',',packages)}" :
                            "packages is null");

                        _reporter.WriteLine(folders != null ?
                            $"folders: {string.Join(',',folders)}" :
                            "folders is null");

                        bool foundIssues = false;
                        foreach(var f in folders) {
                            // finding folders under f that has a .template.config folder
                            var foundDirs = Directory.GetDirectories(f,".template.config",new EnumerationOptions{RecurseSubdirectories = true, AttributesToSkip = FileAttributes.System });
                            if(foundDirs == null || foundDirs.Length <= 0) {
                                _reporter.WriteLine($"ERROR: No templates found under path '{f}'");
                            }
                            foreach(var fd in foundDirs) {
                                foundIssues = _templateAnalyzer.Analyze(Directory.GetParent(fd).FullName) || foundIssues;
                            }
                        }

                        return foundIssues ? -1 : 0;
                }),
                OptionPackages(),
                OptionFolders(),
            };

        protected Option OptionPackages() =>
            new Option(new string[] { "--packages", "-p" }, "search for nuget package files (.nupkg)") {
                Argument = new Argument<string[]>()
            };

        protected Option OptionFolders() =>
            new Option(new string[] { "--folders", "-f" }, "search for templates in sub-folders") {
                Argument = new Argument<string[]>()
            };
    }
}

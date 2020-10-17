using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text;
using TemplatesShared;

namespace Templates {
    /// <summary>
    /// Analyzes .net core templates
    /// </summary>
    public class AnalyzeTemplateCommand : CommandBase {
        private IReporter _reporter;

        public AnalyzeTemplateCommand(IReporter reporter) {
            Debug.Assert(reporter != null);

            _reporter = reporter;
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

                CommandHandler.Create<string,bool,bool>((path, analyzePackages, analyzeFolders) => {
                    _reporter.WriteLine("analyze command");
                    _reporter.WriteLine($"path: {path}");
                    _reporter.WriteLine($"analyzePackages: {analyzePackages}");
                    _reporter.WriteLine($"analyzeFolders: {analyzeFolders}");
                }),
                ArgPath(),
                OptionPackages(),
                OptionFolders()
            };

        protected Argument ArgPath() =>
            new Argument<string>(name: "path", description: "path to inspect for templates");

        protected Option<bool> OptionPackages() =>
            new Option<bool>(new string[] { "--packages", "-p" }, "search for nuget package files (.nupkg)");

        protected Option<bool> OptionFolders() =>
            new Option<bool>(new string[] { "--folders", "-f" }, "search for templates in sub-folders");
    }
}

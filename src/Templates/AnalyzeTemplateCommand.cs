﻿using System;
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
        private IRemoteFile _remoteFile;

        public AnalyzeTemplateCommand(IReporter reporter, ITemplateAnalyzer analyzer, IRemoteFile remoteFile) :base() {
            Debug.Assert(reporter != null);
            Debug.Assert(analyzer != null);

            _reporter = reporter;
            _templateAnalyzer = analyzer;
            _remoteFile = remoteFile;
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

                CommandHandler.Create<string[],string[],bool,bool>(
                    (packages, folders, enableVerbose, usePackageCache) => {
                        _reporter.EnableVerbose = enableVerbose;
                        _reporter.WriteLine("analyzing...");

                        _reporter.WriteVerboseLine(packages != null ?
                            $"packages: {string.Join(',',packages)}" :
                            "packages is null");

                        _reporter.WriteVerboseLine(folders != null ?
                            $"folders: {string.Join(',',folders)}" :
                            "folders is null");

                        var foldersList = new List<string>();
                        if(folders != null && folders.Length > 0)
                        {
                            foldersList.AddRange(folders);
                        }
                        else
                        {
                            _reporter.WriteLine("no folders found to analyze");
                        }

                        if(packages != null && packages.Length > 0)
                        {
                            foreach(var p in packages)
                            {
                                // check that the path exists and then extract to a folder
                                if (File.Exists(p))
                                {
                                    _reporter.WriteVerbose($"extracting package '{p}'");
                                    var packageFolder = _remoteFile.ExtractZipLocally(p, !usePackageCache);
                                    foldersList.Add(packageFolder);
                                }
                                else
                                {
                                    _reporter.WriteLine($"ERROR: package not found at '{p}'");
                                }
                            }
                        }
                        //else
                        //{
                        //    _reporter.WriteLine("no packages found to analyze");
                        //}

                        bool foundIssues = false;
                        if(foldersList != null && foldersList.Count > 0){
                            foreach(var f in foldersList) {
                                // finding folders under f that has a .template.config folder
                                var foundDirs = Directory.GetDirectories(f,".template.config",new EnumerationOptions{RecurseSubdirectories = true });
                                if(foundDirs == null || foundDirs.Length <= 0) {
                                    _reporter.WriteLine($"ERROR: No templates found under path '{f}'");
                                }
                                foreach(var fd in foundDirs) {
                                    foundIssues = _templateAnalyzer.Analyze(Directory.GetParent(fd).FullName) || foundIssues;
                                }
                            }
                        }

                        return foundIssues ? -1 : 0;
                }),
                OptionPackages(),
                OptionFolders(),
                OptionVerbose(),
                OptionUsePackageCache()
            };

        protected Option OptionPackages() =>
            new Option(new string[] { "--packages", "-p" }, "search for nuget package files (.nupkg)") {
                Argument = new Argument<string[]>()
            };

        protected Option OptionFolders() =>
            new Option(new string[] { "--folders", "-f" }, "search for templates in sub-folders") {
                Argument = new Argument<string[]>()
            };

        protected Option OptionUsePackageCache() =>
            new Option(new string[] { "--use-cache" }, "when expanding nuget packages the package cache should be used. (Only used when -p|--packages is passed as a paramter)") {
                Argument = new Argument<bool>(name: "usepackagecache")
    };
    }
}

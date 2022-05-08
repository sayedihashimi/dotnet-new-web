using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using TemplatesShared;
using Task = Microsoft.Build.Utilities.Task;

namespace SayedHa.Templates.Tasks {
    public class AnalyzeTemplate : Task {

        public ITaskItem[]? Folders { get; set; }
        public ITaskItem[]? Packages { get; set; }
        public string? CacheFolderpath { get; set; }
        public bool UsePackageCache { get; set; } = true;

        public bool AttachDebugger { get; set; } = false;

        public override bool Execute() {
#if DEBUG
            // System.Threading.Tasks.Task.Delay(1000).Wait();
#endif
            if (AttachDebugger) {
                Log.LogMessage(MessageImportance.High, "trying to attach the debugger");
                System.Diagnostics.Debugger.Launch();
            }

            var remoteFile = new RemoteFile();
            if (!string.IsNullOrWhiteSpace(CacheFolderpath)) {
                Log.LogMessage(MessageImportance.Low, $"{nameof(CacheFolderpath)} passed in '{CacheFolderpath}'");
                if (!Directory.Exists(CacheFolderpath)) {
                    Log.LogMessage(MessageImportance.Low, $"Creating cache folder at '{CacheFolderpath}'");
                    Directory.CreateDirectory(CacheFolderpath);
                }
            }

            // build a list of folders to analyze, nuget packages need to be extracted to a folder
            var folderList = new List<string>();

            if (Folders != null && Folders.Length > 0) {
                foreach (var folder in Folders) {
                    var folderPath = folder.GetMetadata("Fullpath");
                    Log.LogMessage(MessageImportance.Low, $"folder specified: '{folderPath}'");
                    folderList.Add(folderPath);
                }
            }

            if (Packages != null && Packages.Length > 0) {
                foreach (var package in Packages) {
                    var pkgFullPath = package.GetMetadata("Fullpath");
                    Log.LogMessage(MessageImportance.Low,$"package specified: '{pkgFullPath}'");
                    if (File.Exists(pkgFullPath)) {
                        // extract the package locally
                        var packageFolder = remoteFile.ExtractZipLocally(pkgFullPath, !UsePackageCache);
                        folderList.Add(packageFolder);
                    }
                    else {
                        Log.LogWarning($"pkg to analyze not found at '{pkgFullPath}'");
                    }
                }
            }

            // var analyzeResult = new AnalyzeResult();
            var analyzer = new TemplateJsonPathAnalyzer(new MSBuildReporter(Log), new JsonHelper());
            if (folderList.Count > 0) {
                foreach (var f in folderList) {
                    var foundDirs = Directory.GetDirectories(f, ".template.config", SearchOption.AllDirectories);
                    if(foundDirs == null || foundDirs.Length <= 0) {
                        Log.LogWarning($"No templates found to analyze under path '{f}'");
                    }
                    else {
                        foreach(var fd in foundDirs) {
                            Log.LogMessage(MessageImportance.Normal, $"analyzing template folder: '{fd}'");
                            var pathToAnalyze = Directory.GetParent(fd).FullName;
                            LogResults(analyzer.Analyze(pathToAnalyze), Path.Combine(pathToAnalyze, "template.config"));
                            
                            //analyzeResult = AnalyzeResult.Combine(
                            //    analyzeResult,
                            //    analyzer.Analyze(Directory.GetParent(fd).FullName));
                        }
                    }
                }
            }
            else {
                Log.LogWarning($"Nothing specified to analyze, either Packages or Folders should be passed to this task.");
            }
            
            // TODO: do we need to do anything with the result here?

            return !Log.HasLoggedErrors;
        }
        protected internal void LogResults(AnalyzeResult result, string pathBeingAnalyzed) {
            if (result?.Issues?.Count == 0) {
                return;
            }

            foreach(var issue in result.Issues) {
                switch (issue.IssueType) {
                    case ErrorWarningType.Info:
                        Log.LogMessage(issue.IssueMessage);
                        break;
                    case ErrorWarningType.Warning:
                        Log.LogWarning(null, null, null, pathBeingAnalyzed, 0, 0, 0, 0, issue.IssueMessage);
                        break;
                    case ErrorWarningType.Error:
                        Log.LogError(null, null, null, pathBeingAnalyzed, 0, 0, 0, 0, issue.IssueMessage);
                        break;
                    default:
                        Log.LogMessage(null, null, null, pathBeingAnalyzed, 0, 0, 0, 0, issue.IssueMessage);
                        break;
                }
            }
        }
    }
}
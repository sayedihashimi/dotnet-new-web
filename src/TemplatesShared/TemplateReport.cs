using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using System.Reflection;

namespace TemplatesShared {
    public class TemplateReport {
        private readonly INuGetHelper _nugetHelper;
        private readonly HttpClient _httpClient;
        private readonly INuGetPackageDownloader _nugetDownloader;
        private readonly IRemoteFile _remoteFile;

        public bool EnableVerbose{get;set;}

        public TemplateReport(INuGetHelper nugetHelper, HttpClient httpClient, INuGetPackageDownloader nugetDownloader, IRemoteFile remoteFile) {
            Debug.Assert(nugetHelper != null);
            Debug.Assert(httpClient != null);
            Debug.Assert(nugetDownloader != null);
            Debug.Assert(remoteFile != null);

            _nugetHelper = nugetHelper;
            _httpClient = httpClient;
            _nugetDownloader = nugetDownloader;
            _remoteFile = remoteFile;
        }
        public async Task GenerateTemplateJsonReportAsync(string[] searchTerms, string jsonReportFilepath, List<string> specificPackagesToInclude, string previousReportPath) {
            Debug.Assert(searchTerms != null && searchTerms.Length > 0);
            Debug.Assert(!string.IsNullOrEmpty(jsonReportFilepath));

            
            Dictionary<string, TemplatePack> previousPacks = new Dictionary<string, TemplatePack>();
            if (!string.IsNullOrEmpty(previousReportPath) && File.Exists(previousReportPath)) {
                List<TemplatePack> previousReport = new List<TemplatePack>();
                previousReport = JsonConvert.DeserializeObject<List<TemplatePack>>(File.ReadAllText(previousReportPath));
                previousPacks = TemplatePack.ConvertToDictionary(previousReport);
            }

            // 1: query nuget for search results, we need to query all because we need to get the new download count
            var foundPackages = await _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, specificPackagesToInclude, GetPackagesToIgnore());
            var pkgsToDownload = new List<NuGetPackage>();
            // go through each found package, if pkg is in previous result with same version number, update download count and move on
            // if not same version number, remove from dictionary and add to list to download
            foreach(var pkg in foundPackages) {
                var id = TemplatePack.NormalisePkgId(pkg.Id);
                if (previousPacks.ContainsKey(id)) {
                    var previousPackage = previousPacks[id];
                    // check version number to see if it is the same
                    if(string.Compare(pkg.Version, previousPackage.Version, StringComparison.OrdinalIgnoreCase) == 0) {
                        // same version just update the download count
                        previousPackage.DownloadCount = pkg.TotalDownloads;
                    }
                    else {
                        previousPacks.Remove(id);
                        pkgsToDownload.Add(pkg);
                    }
                }
                else {
                    pkgsToDownload.Add(pkg);
                }
            }

            // 2: download nuget packages locally
            // var downloadedPackages = await _nugetDownloader.DownloadAllPackagesAsync(foundPackages);
            var downloadedPackages = await _nugetDownloader.DownloadAllPackagesAsync(pkgsToDownload);

            // 3: extract nuget package to local folder
            var templatePackages = new List<NuGetPackage>();
            var listPackagesWithNotemplates = new List<NuGetPackage>();
            var pkgNamesWitoutPackages = new List<string>();
            foreach(var pkg in downloadedPackages) {
                var extractPath = _remoteFile.ExtractZipLocally(pkg.LocalFilepath);
                pkg.LocalExtractPath = extractPath;
                // see if there is a .template
                var foundDirs  = Directory.EnumerateDirectories(extractPath, ".template.config", new EnumerationOptions { RecurseSubdirectories = true });
                if(foundDirs.Count() > 0) {
                    templatePackages.Add(pkg);
                }
                else {
                    Console.WriteLine($"pkg has no templates: {pkg.Id}");
                    listPackagesWithNotemplates.Add(pkg);
                    pkgNamesWitoutPackages.Add(pkg.Id);
                }
            }

            var reportsPath = Path.Combine(_remoteFile.CacheFolderpath, "reports",DateTime.Now.ToString("MM.dd.yy-H.m.s.ffff"));
            if (!Directory.Exists(reportsPath)) {
                Directory.CreateDirectory(reportsPath);
            }
            
            File.WriteAllText(
                    Path.Combine(reportsPath, "newly-found-packages-without-templates.json"),
                    JsonConvert.SerializeObject(listPackagesWithNotemplates,Formatting.Indented));

            File.WriteAllText(
                    Path.Combine(reportsPath, "newly-found-package-names-to-ignore.json"),
                    JsonConvert.SerializeObject(pkgNamesWitoutPackages, Formatting.Indented));

            var templatePacks = new List<TemplatePack>();
            foreach(var pkg in templatePackages) {
                // get nuspec file path
                var nuspecFile = Directory.GetFiles(pkg.LocalExtractPath, $"{pkg.Id}.nuspec").FirstOrDefault();
                if(nuspecFile == null) {
                    Console.WriteLine($"warning: nuspec not found in folder {pkg.LocalFilepath}");
                    continue;
                }
                // get template folders
                var contentDir = Path.Combine(pkg.LocalExtractPath);
                if (!Directory.Exists(contentDir)) {
                    continue;
                }
                var templateFolders = Directory.GetDirectories(contentDir, ".template.config", SearchOption.AllDirectories);
                var templateFiles = new List<string>();
                foreach(var folder in templateFolders) {
                    var files = Directory.GetFiles(folder, "template.json", new EnumerationOptions { RecurseSubdirectories = true });
                    if(files != null && files.Length > 0) {
                        templateFiles.AddRange(files);
                    }
                }

                try {
                    var tp = TemplatePack.CreateFromNuSpec(pkg, nuspecFile, templateFiles);
                    templatePacks.Add(tp);
                }
                catch (Exception ex) {
                    Console.WriteLine($"error creating template pack {nuspecFile} {ex.ToString()}");
                }
            }

            // add all the downloaded items to existing dictionary then get the full result
            foreach(var pkg in templatePacks) {
                var id = TemplatePack.NormalisePkgId(pkg.Package);
                if (previousPacks.ContainsKey(id)) {
                    // I believe it shouldn't get here, but just in case
                    previousPacks.Remove(id);
                }

                previousPacks.Add(id, pkg);
            }

            templatePacks = previousPacks.Values.ToList();

            templatePacks = templatePacks.OrderBy((tp) => -1 * tp.DownloadCount).ToList();
            // write to cache folder and then copy to dest
            var cacheFile = Path.Combine(reportsPath, "template-report.json");
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(templatePacks, Formatting.Indented));
            if (File.Exists(jsonReportFilepath)) {
                File.Delete(jsonReportFilepath);
            }

            Console.WriteLine($"Writing report to '{jsonReportFilepath}'");
            File.Copy(cacheFile, jsonReportFilepath);
        }

        protected List<string> GetPackagesToIgnore() {
            // read it from the file
            var pathToIgnoreFile = Path.Combine(
                    new FileInfo(new System.Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).Directory.FullName,
                    "package-names-to-ignore.json");
            if (File.Exists(pathToIgnoreFile)) {
                var ignoreJson = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(pathToIgnoreFile));
                var result = ignoreJson.ToList();
                return result;
            }

            return new List<string>();
        }

        private void WriteVerbose(string str) {
            if (EnableVerbose) {
                Console.Write("verbose: ");
                Console.WriteLine(str);
            }
        }
    }

    public class NuGetPackageDownloader : INuGetPackageDownloader {
        // TODO: un-hardcode the num of consumers/producers
        private readonly INuGetHelper _nugetHelper;
        private readonly IRemoteFile _remoteFile;
        public NuGetPackageDownloader(INuGetHelper nugetHelper, IRemoteFile remoteFile) {
            _nugetHelper = nugetHelper;
            _remoteFile = remoteFile;
        }
        public async Task<List<NuGetPackage>> DownloadAllPackagesAsync(List<NuGetPackage> packageList) {
            Debug.Assert(packageList != null && packageList.Count > 0);

            // urls to download
            // TODO: dont hard code the num of producer/consumer
            List<NuGetPackage> downloadedPackages1 = new List<NuGetPackage>();
            List<NuGetPackage> downloadedPackages2 = new List<NuGetPackage>();
            List<NuGetPackage> downloadedPackages3 = new List<NuGetPackage>();
            List<NuGetPackage> downloadedPackages4 = new List<NuGetPackage>();
            List<NuGetPackage> downloadedPackages5 = new List<NuGetPackage>();
            // TODO: rename
            BufferBlock<NuGetPackage> urlDownloadQueue = new BufferBlock<NuGetPackage>(new DataflowBlockOptions { BoundedCapacity = 5 });
            var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1 };

            // todo: needs to be refactored
            var consumer1 = new ActionBlock<NuGetPackage>(async pkg => {
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localfilepath = await _remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());
                pkg.LocalFilepath = localfilepath;
                downloadedPackages1.Add(pkg);
            }, consumerOptions);
            var consumer2 = new ActionBlock<NuGetPackage>(async pkg => {
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localfilepath = await _remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());
                pkg.LocalFilepath = localfilepath;
                downloadedPackages2.Add(pkg);
            }, consumerOptions);
            var consumer3 = new ActionBlock<NuGetPackage>(async pkg => {
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localfilepath = await _remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());
                pkg.LocalFilepath = localfilepath;
                downloadedPackages3.Add(pkg);
            }, consumerOptions);
            var consumer4 = new ActionBlock<NuGetPackage>(async pkg => {
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localfilepath = await _remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());
                pkg.LocalFilepath = localfilepath;
                downloadedPackages4.Add(pkg);
            }, consumerOptions);
            var consumer5 = new ActionBlock<NuGetPackage>(async pkg => {
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localfilepath = await _remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());
                pkg.LocalFilepath = localfilepath;
                downloadedPackages5.Add(pkg);
            }, consumerOptions);

            urlDownloadQueue.LinkTo(consumer1, new DataflowLinkOptions { PropagateCompletion = true });
            urlDownloadQueue.LinkTo(consumer2, new DataflowLinkOptions { PropagateCompletion = true });
            urlDownloadQueue.LinkTo(consumer3, new DataflowLinkOptions { PropagateCompletion = true });
            urlDownloadQueue.LinkTo(consumer4, new DataflowLinkOptions { PropagateCompletion = true });
            urlDownloadQueue.LinkTo(consumer5, new DataflowLinkOptions { PropagateCompletion = true });

            var producer = ProduceAsync(urlDownloadQueue, packageList);
            //var consumer = ConsumeAsync(urlDownloadQueue, _remoteFile);

            await Task.WhenAll(producer, consumer1.Completion, consumer2.Completion, consumer3.Completion, consumer4.Completion, consumer5.Completion);

            var allResults = downloadedPackages1.Concat(downloadedPackages2).Concat(downloadedPackages3).Concat(downloadedPackages4).Concat(downloadedPackages5).ToList();

            return allResults;
        }

        protected async Task ProduceAsync(BufferBlock<NuGetPackage> packageQueue, List<NuGetPackage> packagesList) {
            foreach (var pkg in packagesList) {
                await packageQueue.SendAsync<NuGetPackage>(pkg);
            }

            packageQueue.Complete();
        }

        protected async Task<IEnumerable<NuGetPackage>> ConsumeAsync(BufferBlock<NuGetPackage> packkageQueue, IRemoteFile remoteFile) {
            var resultList = new List<NuGetPackage>();

            while (await packkageQueue.OutputAvailableAsync()) {
                var pkg = await packkageQueue.ReceiveAsync<NuGetPackage>();
                var downloadUrl = _nugetHelper.GetDownloadUrlFor(pkg);
                var localFilepath = await remoteFile.GetRemoteFileAsync(downloadUrl, pkg.GetPackageFilename());

                pkg.LocalFilepath = localFilepath;
                resultList.Add(pkg);
            }

            return resultList;
        }
    }
}

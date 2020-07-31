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

namespace TemplatesShared {
    public class TemplateReport {
        private INuGetHelper _nugetHelper;
        private HttpClient _httpClient;
        private INuGetPackageDownloader _nugetDownloader;
        private IRemoteFile _remoteFile;

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
        // TODO: add the ability to specify specific nuget package IDs to include.
        // Some are not included in search results for some reason
        // https://api.nuget.org/v3/registration3/amazon.lambda.templates/index.json
        public async Task GenerateTemplateJsonReportAsync(string[] searchTerms, string jsonReportFilepath) {
            Debug.Assert(searchTerms != null && searchTerms.Length > 0);
            Debug.Assert(!string.IsNullOrEmpty(jsonReportFilepath));

            // 1: query nuget for search results
            var foundPackages = await _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, GetPackagesToIgnore());
            // 2: download nuget packages locally
            var downloadedPackages = await _nugetDownloader.DownloadAllPackagesAsync(foundPackages);

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
            Path.Combine(reportsPath, "packages-without-templates.json");
            Path.Combine(reportsPath, "package-names-without-templates.txt");

            File.WriteAllText(
                    Path.Combine(reportsPath, "packages-without-templates.json"),
                    Newtonsoft.Json.JsonConvert.SerializeObject(listPackagesWithNotemplates));

            File.WriteAllText(
                    Path.Combine(reportsPath, "package-names-without-templates.txt"),
                    Newtonsoft.Json.JsonConvert.SerializeObject(pkgNamesWitoutPackages));

            var templatePacks = new List<TemplatePack>();
            foreach(var pkg in templatePackages) {
                // get nuspec file path
                var nuspecFile = Directory.GetFiles(pkg.LocalExtractPath, $"{pkg.Id}.nuspec").FirstOrDefault();
                if(nuspecFile == null) {
                    Console.WriteLine($"warning: nuspec not found in folder {pkg.LocalFilepath}");
                    continue;
                }
                // get template folders
                var contentDir = Path.Combine(pkg.LocalExtractPath, "content");
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

            templatePacks = templatePacks.OrderBy((tp) => -1 * tp.DownloadCount).ToList();
            // write to cache folder and then copy to dest
            var cacheFile = Path.Combine(reportsPath, "template-report.json");
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(templatePacks));
            if (File.Exists(jsonReportFilepath)) {
                File.Delete(jsonReportFilepath);
            }
            File.Copy(cacheFile, jsonReportFilepath);
        }

        // todo: improve this
        protected List<string> GetPackagesToIgnore() {
            return Strings.PackagesToIgnore.Split("\n").ToList();
        }

        private void ReportPackages(List<NuGetPackage> packages) {
            if(!EnableVerbose || packages == null || packages.Count <= 0) {
                return;
            }
            
            foreach(var pkg in packages) {
                WriteVerbose($"found {pkg.Id}");
            }
        }

        private void WriteVerbose(string str) {
            if (EnableVerbose) {
                Console.Write("verbose: ");
                Console.WriteLine(str);
            }
        }
    }

    public class NuGetPackageDownloader : INuGetPackageDownloader {
        private int numDownloaders = 5;
        private INuGetHelper _nugetHelper;
        private IRemoteFile _remoteFile;
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TemplatesShared {
    public class TemplateReport {
        private INuGetHelper _nugetHelper;
        private HttpClient _httpClient;
        public TemplateReport(INuGetHelper nugetHelper, HttpClient httpClient) {
            Debug.Assert(nugetHelper != null);
            Debug.Assert(httpClient != null);

            _nugetHelper = nugetHelper;
            _httpClient = httpClient;
        }
        public void GenerateTemplateJsonReport(string[] searchTerms, string jsonReportFilepath) {
            Debug.Assert(searchTerms != null && searchTerms.Length > 0);
            Debug.Assert(!string.IsNullOrEmpty(jsonReportFilepath));

            // 1: query nuget for search results
            var foundPackages = _nugetHelper.QueryNuGetAsync(_httpClient, searchTerms, GetPackagesToIgnore());
            // 2: download nuget packages locally

            // 3: extract nuget package to local folder
            // 4: look into extract folder for a template json file


            throw new NotImplementedException();
        }

        // todo: improve this
        protected List<string> GetPackagesToIgnore() {
            return Strings.PackagesToIgnore.Split("\n").ToList();
        }
    }

    public class NuGetPackageDownloader {
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
            BufferBlock<NuGetPackage> urlDownloadQueue = new BufferBlock<NuGetPackage>();
            var producer = ProduceAsync(urlDownloadQueue, packageList);
            var consumer = ConsumeAsync(urlDownloadQueue, _remoteFile);

            await Task.WhenAll(producer, consumer, urlDownloadQueue.Completion);
            var result = await consumer;

            return result.ToList();
        }

        protected async Task ProduceAsync(BufferBlock<NuGetPackage> packageQueue, List<NuGetPackage> packagesList) {
            foreach(var pkg in packagesList) {
                await packageQueue.SendAsync<NuGetPackage>(pkg);
            }

            packageQueue.Complete();
        }

        protected async Task<IEnumerable<NuGetPackage>> ConsumeAsync(BufferBlock<NuGetPackage> packkageQueue, IRemoteFile remoteFile) {
            var resultList = new List<NuGetPackage>();

            while(await packkageQueue.OutputAvailableAsync()) {
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

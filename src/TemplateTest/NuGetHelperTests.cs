using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TemplatesShared;
using Xunit;

namespace TemplateTest {
    public class NuGetHelperTests {
        private HttpClient httpClient = new HttpClient();
        [Fact]
        public async Task TestGetAllQueryUrisAsync01Async() {
            var nugetApiHelper = new NuGetHelper(new RemoteFile());

            var results = await nugetApiHelper.GetAllQueryUrisAsync(httpClient, "template", 20);
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task TestQueryNuGetAsync01Async() {
            var nugetApiHelper = new NuGetHelper(new RemoteFile());

            var results = await nugetApiHelper.QueryNuGetAsync(httpClient, "dec");
            Assert.True(results.Count > 300);
        }
    }
    public class NuGetPackageDownloaderTests {
        private HttpClient httpClient;
        private IRemoteFile remoteFile;
        private INuGetHelper nugetHelper;
        private INuGetPackageDownloader nugetDownloader;
        public NuGetPackageDownloaderTests() {
            httpClient = new HttpClient();
            remoteFile = new RemoteFile();
            nugetHelper = new NuGetHelper(remoteFile);
            nugetDownloader = new NuGetPackageDownloader(nugetHelper, remoteFile);
        }

        [Fact]
        public async Task TestDownloadAllPackagesAsync01Async() {
            var packages = await nugetHelper.QueryNuGetAsync(httpClient, "dec");
            var downloader = new NuGetPackageDownloader(nugetHelper, remoteFile);

            var result = await downloader.DownloadAllPackagesAsync(packages);

            Assert.NotNull(result);
            Assert.Equal(packages.Count, result.Count);
            Assert.True(!string.IsNullOrEmpty(packages[0].LocalFilepath));
            Assert.True(File.Exists(packages[0].LocalFilepath));
        }

        [Fact]
        public async Task TestTemplateReport01Async() {
            var templateReport = new TemplateReport(nugetHelper, httpClient, nugetDownloader, remoteFile);
            await templateReport.GenerateTemplateJsonReportAsync(new string[] { "template" }, @"");

            Assert.True(1 == 1);
        }


        [Fact]
        public void TestNuspecReader01() {
            // todo: fix to remove this
            string nuspecfilepath = @"C:\Users\sayedha\AppData\Local\templatereport\extracted\microsoft.dotnet.web.projecttemplates.2.2.2.2.8.nupkg\Microsoft.DotNet.Web.ProjectTemplates.2.2.nuspec";
            NuspecFile nuspec = NuspecFile.CreateFromNuspecFile(nuspecfilepath);

            Assert.NotNull(nuspec);
            Assert.True(!string.IsNullOrEmpty(nuspec.Metadata.Id));
        }
        [Fact]
        public void TestCreateTemplatePack01() {
            string nuspecfilepath = @"C:\Users\sayedha\AppData\Local\templatereport\extracted\microsoft.dotnet.web.projecttemplates.2.2.2.2.8.nupkg\Microsoft.DotNet.Web.ProjectTemplates.2.2.nuspec";
            string[]templateFiles = Directory.GetFiles(Path.GetDirectoryName(nuspecfilepath), "template.json",new EnumerationOptions { RecurseSubdirectories = true });

            var templatePack = TemplatePack.CreateFromNuSpec(nuspecfilepath, templateFiles.ToList());

            Assert.NotNull(templatePack);
        }
    }
}

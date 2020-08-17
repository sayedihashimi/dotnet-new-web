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
        private readonly HttpClient httpClient = new HttpClient();
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
        private readonly HttpClient httpClient;
        private readonly IRemoteFile remoteFile;
        private readonly INuGetHelper nugetHelper;
        private readonly INuGetPackageDownloader nugetDownloader;
        private readonly SampleFileHelper sampleFileHelper;
        public NuGetPackageDownloaderTests() {
            httpClient = new HttpClient();
            remoteFile = new RemoteFile();
            nugetHelper = new NuGetHelper(remoteFile);
            nugetDownloader = new NuGetPackageDownloader(nugetHelper, remoteFile);
            sampleFileHelper = new SampleFileHelper();
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

        // takes a long time to run
        //[Fact]
        //public async Task TestTemplateReport01Async() {
        //    var templateReport = new TemplateReport(nugetHelper, httpClient, nugetDownloader, remoteFile);
        //    await templateReport.GenerateTemplateJsonReportAsync(new string[] { "template" }, @"");

        //    Assert.True(1 == 1);
        //}
        [Fact]
        public void TestCreateTemplateFromJson() {
            string templateJsonPath = Path.Combine(sampleFileHelper.GetSamplesFolder(), "templatejson", "template01.json");

            var result = TemplatePack.CreateTemplateFromJsonFile(templateJsonPath, "test-case");
            Assert.NotNull(result);
            Assert.True(!string.IsNullOrEmpty(result.Identity));
            Assert.NotNull(result.Tags);
        }

        [Fact]
        public void TestNuspecReader01() {
            var nuspecFiles = new List<string> {
                Path.Combine(sampleFileHelper.GetSamplesFolder(), "nuspec", "Fable.Template.Elmish.React.nuspec"),
                Path.Combine(sampleFileHelper.GetSamplesFolder(), "nuspec", "DevExpress.DotNet.Web.ProjectTemplates.nuspec")
            };

            foreach(var filepath in nuspecFiles) {
                NuspecFile nuspec = NuspecFile.CreateFromNuspecFile(filepath);
                Assert.NotNull(nuspec);
                Assert.True(!string.IsNullOrEmpty(nuspec.Metadata.Id));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
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
}

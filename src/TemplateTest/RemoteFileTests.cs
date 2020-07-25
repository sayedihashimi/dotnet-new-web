using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TemplatesShared;
using Xunit;

namespace TemplateTest {
    public class RemoteFileTests {

        [Fact]
        public void TestGetRemoteFileThatIsNotPresentLocally() {
            // todo: would be better to externalize the download part so that can be mocked for test
            string urlToDownload = @"https://raw.githubusercontent.com/sayedihashimi/dotnet-new-web/master/src/template-report.json";
            string filename = Guid.NewGuid().ToString();
            var remoteFile = new RemoteFile();
            var localFile = remoteFile.GetRemoteFile(urlToDownload, filename);

            Assert.True(File.Exists(localFile), $"local file not found at '{localFile}'");
        }
        [Fact]
        public void TestGetRemoteFileThatIsPresentLocally() {
            string urltodownload = @"https://raw.githubusercontent.com/sayedihashimi/dotnet-new-web/master/src/template-report.json";
            string filename = "knownfilename.txt";
            var remoteFile = new RemoteFile();
            // make sure that the file exists in the cachefolder
            var expectedFilepath = remoteFile.GetLocalFilepathFor(filename);
            if (!File.Exists(expectedFilepath)) {
                // create a dummy file
                File.WriteAllText(expectedFilepath, "12345");
            }
        }
    }
}

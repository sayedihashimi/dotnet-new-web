using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace TemplatesShared {
    public interface IRemoteFile {
        bool Verbose { get; set; }

        string GetRemoteFile(string downloadUrl, string filename, bool forceDownload = false);
        string GetRemoteFile(Uri uri, string filename, bool forceDownlaod = false);
        void SetCacheFolderpath(string folderpath);
    }

    public class RemoteFile : IRemoteFile {
        protected internal string CacheFolderpath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "templatereport");
        // todo: should have a better way to conditionally print to the console
        public bool Verbose { get; set; }
        public void SetCacheFolderpath(string folderpath) {
            if (string.IsNullOrEmpty(folderpath)) {
                throw new ArgumentNullException(nameof(folderpath), $"{nameof(folderpath)} is empty");
            }

            CacheFolderpath = folderpath;
        }

        public string GetRemoteFile(string downloadUrl, string filename, bool forceDownload = false) {
            if (string.IsNullOrEmpty(downloadUrl)) {
                throw new ArgumentNullException(nameof(downloadUrl), $"{nameof(downloadUrl)} is empty");
            }
            return GetRemoteFile(new Uri(downloadUrl), filename, forceDownload);
        }
        public string GetRemoteFile(Uri uri, string filename, bool forceDownlaod = false) {
            // check to see if the file has already been downloaded
            var expectedFilePath = GetLocalFilepathFor(filename);
            if (File.Exists(expectedFilePath) && forceDownlaod) {
                File.Delete(expectedFilePath);
            }

            // if the file exists at the expected location return it
            if (File.Exists(expectedFilePath)) {
                return expectedFilePath;
            }

            if (!Directory.Exists(Path.GetDirectoryName(expectedFilePath))) {
                Directory.CreateDirectory(Path.GetDirectoryName(expectedFilePath));
            }

            // download the file now
            var webClient = new System.Net.WebClient();
            webClient.DownloadFile(uri, expectedFilePath);

            return expectedFilePath;
        }

        protected internal string GetLocalFilepathFor(string filename) {
            return Path.Combine(CacheFolderpath, filename);
        }
    }
}

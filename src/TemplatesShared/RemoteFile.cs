using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TemplatesShared {
    public interface IRemoteFile {
        bool Verbose { get; set; }
        string CacheFolderpath { get; }

        string ExtractZipLocally(string zipfilepath);
        Task<string> GetRemoteFileAsync(string downloadUrl, string filename, bool forceDownload = false);
        Task<string> GetRemoteFileAsync(Uri uri, string filename, bool forceDownlaod = false);

        //string GetRemoteFileAsync(string downloadUrl, string filename, bool forceDownload = false);
        //string GetRemoteFileAsync(Uri uri, string filename, bool forceDownlaod = false);
        void SetCacheFolderpath(string folderpath);
    }

    public class RemoteFile : IRemoteFile {
        public string CacheFolderpath { get; protected set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "templatereport");
        // todo: should have a better way to conditionally print to the console
        public bool Verbose { get; set; }
        public void SetCacheFolderpath(string folderpath) {
            if (string.IsNullOrEmpty(folderpath)) {
                throw new ArgumentNullException(nameof(folderpath), $"{nameof(folderpath)} is empty");
            }

            CacheFolderpath = folderpath;
        }

        public async Task<string> GetRemoteFileAsync(string downloadUrl, string filename, bool forceDownload = false) {
            if (string.IsNullOrEmpty(downloadUrl)) {
                throw new ArgumentNullException(nameof(downloadUrl), $"{nameof(downloadUrl)} is empty");
            }
            return await GetRemoteFileAsync(new Uri(downloadUrl), filename, forceDownload);
        }
        public async Task<string> GetRemoteFileAsync(Uri uri, string filename, bool forceDownlaod = false) {
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
            // webClient.DownloadFile(uri, expectedFilePath);
            webClient.DownloadFileAsync(uri, expectedFilePath);

            var isDownloadComplete = false;
            webClient.DownloadFileCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) => {
                isDownloadComplete = true;
            };

            int timeoutSec = 60;
            bool timeoutPassed = false;
            var startTime = DateTime.Now;
            // wait until the file is downloaded
            do {
                await Task.Delay(10 * 1000);
                var elapsedTime = (DateTime.Now).Subtract(startTime);
                timeoutPassed = elapsedTime.Seconds > timeoutSec;
            } while (!isDownloadComplete && !timeoutPassed);

            return expectedFilePath;
        }

        protected internal string GetLocalFilepathFor(string filename) {
            return Path.Combine(CacheFolderpath, filename);
        }

        public string ExtractZipLocally(string zipfilepath) {
            if (!File.Exists(zipfilepath)) { throw new ExtractZipException($"zip file not found at '{zipfilepath}'"); }

            var fi = new FileInfo(zipfilepath);

            // check to see if the file has already been extracted to the cachefolder
            var expectedFolderPath = Path.Combine(CacheFolderpath, "extracted", fi.Name);
            if (Directory.Exists(expectedFolderPath)) {
                return expectedFolderPath;
            }

            // TODO: would be good to have an async api here to call
            // extract the zip to the folder path
            ZipFile.ExtractToDirectory(zipfilepath, expectedFolderPath);

            if (!Directory.Exists(expectedFolderPath)) {
                throw new ExtractZipException($"Unknown error. Unable to extract zip to path '{expectedFolderPath}'");
            }

            return expectedFolderPath;
        }
    }
}

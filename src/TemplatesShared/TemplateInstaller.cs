using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TemplatesShared {
    public interface ITemplateInstaller {
        void InstallPackage(string id);
        void InstallPackage(string id, string version);
    }

    public class TemplateInstaller : ITemplateInstaller {
        private IReporter _reporter;
        public TemplateInstaller(IReporter reporter) {
            _reporter = reporter;
        }
        public void InstallPackage(string id) {
            InstallPackage(id, null);
        }
        public void InstallPackage(string id, string version) {
            // dotnet new --install pkg.id.full
            // dotnet new --install pkg.id.full::version
            string args = $"new --install \"{id}\" ";
            if (!string.IsNullOrEmpty(version)) {
                args += $"::{version}";
            }

            var pinfo = new ProcessStartInfo {
                UseShellExecute = false,
                FileName = "dotnet",
                Arguments = args
            };

            pinfo.FileName = "dotnet";
            pinfo.Arguments = args;            

            _reporter.WriteLine($"installing with command: 'dotnet {pinfo.Arguments}'");

            var process = Process.Start(pinfo);

            process.WaitForExit();

            _reporter.WriteLine("complete");
        }
    }
}

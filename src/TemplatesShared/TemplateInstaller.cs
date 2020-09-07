using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared {
    public interface ITemplateInstaller {
        Task InstallPackageAsync(string id);
        Task InstallPackageAsync(string id, string version);
    }

    public class TemplateInstaller : ITemplateInstaller {
        private IReporter _reporter;
        public TemplateInstaller(IReporter reporter) {
            _reporter = reporter;
        }
        public async Task InstallPackageAsync(string id) {
            await InstallPackageAsync(id, null);
        }
        public async Task InstallPackageAsync(string id, string version) {
            // dotnet new --install pkg.id.full
            // dotnet new --install pkg.id.full::version
            string args = $"new --install \"{id}\" ";
            if (!string.IsNullOrEmpty(version)) {
                args += $"::{version}";
            }

            var pinfo = new ProcessStartInfo {
                UseShellExecute = false,
                FileName = @"dotnet",
                Arguments = args
            };
            pinfo.RedirectStandardOutput = true;

            _reporter.WriteLine($"installing with command: 'dotnet {pinfo.Arguments}'");
            try {
                using var process = Process.Start(pinfo);

                _reporter.Write("installing ...");
                while (!process.HasExited) {
                    _reporter.Write(".");
                    await Task.Delay(100);
                    _ = process.StandardOutput.ReadToEnd();
                    process.Refresh();
                }
                _reporter.WriteLine();

                if (process.ExitCode != 0) {
                    _reporter.WriteLine($"Exited with non-zero exit code: '{process.ExitCode}'");
                }
            }
            catch(Exception ex) {
                _reporter.WriteLine($"ERROR: {ex.ToString()}");
            }

            _reporter.WriteLine("complete");
        }
    }
}

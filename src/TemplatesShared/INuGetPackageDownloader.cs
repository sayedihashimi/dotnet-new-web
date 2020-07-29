using System.Collections.Generic;
using System.Threading.Tasks;

namespace TemplatesShared {
    public interface INuGetPackageDownloader {
        Task<List<NuGetPackage>> DownloadAllPackagesAsync(List<NuGetPackage> packageList);
    }
}
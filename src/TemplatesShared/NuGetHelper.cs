using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared {
    public interface INuGetHelper {
        Task<NuGetSearchApiResult> QueryNuGetAsync(HttpClient httpClient, string query);
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries, List<string> packagesToIgnore);
    }

    public class NuGetHelper : INuGetHelper {

        public async Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries, List<string> packagesToIgnore) {
            Debug.Assert(httpClient != null);
            Debug.Assert(queries != null && queries.Length > 0);

            if (packagesToIgnore == null) {
                packagesToIgnore = new List<string>();
            }
            List<string> packagesToIgnoreNormalized = new List<string>();
            foreach (string pkg in packagesToIgnore) {
                packagesToIgnoreNormalized.Add(Normalize(pkg));
            }

            Dictionary<string, NuGetPackage> foundPkgsMap = new Dictionary<string, NuGetPackage>();
            foreach (string query in queries) {
                try {
                    var found = await QueryNuGetAsync(httpClient, query);
                    if (found?.Packages?.Length <= 0) {
                        continue;
                    }

                    foreach (var pkg in found.Packages) {
                        var key = Normalize(pkg.Id);
                        if (!packagesToIgnoreNormalized.Contains(key) &&
                            !foundPkgsMap.ContainsKey(key)) {
                            foundPkgsMap.Add(key, pkg);
                        }
                    }

                }
                catch (Exception ex) {
                    // log error to console but continue
                    // TODO: should we be continuing?
                    Console.WriteLine(ex.ToString());
                }
            }

            return foundPkgsMap.Values.ToList<NuGetPackage>();

        }
        private string Normalize(string keyStr) {
            return keyStr.ToLowerInvariant();
        }
        public async Task<NuGetSearchApiResult> QueryNuGetAsync(HttpClient httpClient, string query) {
            Uri uri = new Uri(new Uri(Strings.NuGetQueryBaseUrl), $"?q={query}");
            var queryResult = await httpClient.GetStringAsync(uri);
            try {
                var foundPackages = JsonConvert.DeserializeObject<NuGetSearchApiResult>(queryResult);

                return foundPackages;
            }
            catch (Exception ex) {
                throw new NuGetQueryException($"Unable to query nuget. Query uri: '{uri.ToString()}'", ex);

            }
        }
    }
}

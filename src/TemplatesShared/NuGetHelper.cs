using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TemplatesShared {
    public interface INuGetHelper {
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string query);
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries, List<string> packagesToIgnore);
    }

    public class NuGetHelper : INuGetHelper {
        protected int NumPackagesToTake { get; set; } = 20;
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
                    if (found?.Count <= 0) {
                        continue;
                    }

                    foreach (var pkg in found) {
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
        public async Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string query) {
            int skip = 0;

            var initialResult = await ExecuteQueryAsync(httpClient, query);
            var packageResults = new List<NuGetPackage>(initialResult.TotalHits);
            
            packageResults.AddRange(initialResult.Packages);

            if (initialResult.TotalHits < NumPackagesToTake) {
                return packageResults;
            }

            // TODO: perhaps try this https://blog.stephencleary.com/2012/11/async-producerconsumer-queue-using.html



            BlockingCollection<NuGetPackage> packagesInProgress = new BlockingCollection<NuGetPackage>(initialResult.TotalHits);
            // packagesInProgress.TryAdd()
            // BufferBlock<NuGetPackage> foo;

            Uri uri = new Uri(new Uri(Strings.NuGetQueryBaseUrl), $"?q={query}&prerelease=true&take={NumPackagesToTake}&skip={skip}");
            // execute the first query to get the num hits
            var queryResult = await httpClient.GetStringAsync(uri);
            try {
                var foundPackages = JsonConvert.DeserializeObject<NuGetSearchApiResult>(queryResult);

                return foundPackages.Packages.ToList();
            }
            catch (Exception ex) {
                throw new NuGetQueryException($"Unable to query nuget. Query uri: '{uri.ToString()}'", ex);

            }
        }

        protected async Task<NuGetSearchApiResult> ExecuteQueryAsync(HttpClient httpClient, string query, int numRetries = 3) {
            Debug.Assert(httpClient != null);
            Debug.Assert(!string.IsNullOrEmpty(query));
              
            Uri queryUri = new Uri(new Uri(Strings.NuGetQueryBaseUrl), query);

            int numRuns = 0;
            NuGetSearchApiResult result = null;
            while(numRuns < numRetries) {
                try {
                    var resultJson = await httpClient.GetStringAsync(queryUri);
                    result = JsonConvert.DeserializeObject<NuGetSearchApiResult>(resultJson);
                }
                catch(Exception ex) {
                    Console.WriteLine($"warning: {ex.ToString()}");
                }

                numRuns++;
            }

            if(result == null) {
                throw new NuGetQueryException($"Unable to query nuget for uri: {queryUri.ToString()}");
            }

            return result;
        }
    }
}

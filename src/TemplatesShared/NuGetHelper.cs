using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace TemplatesShared {
    public interface INuGetHelper {
        int NumPackagesToTake { get; set; }
        string GetDownloadUrlFor(NuGetPackage pkg);
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string query);
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries, List<string> packagesToIgnore);
    }

    // TODO: rename class and interface
    public class NuGetHelper : INuGetHelper {
        public NuGetHelper(IRemoteFile remoteFile) {
            RemoteFile = remoteFile;
        }

        protected IRemoteFile RemoteFile { get; set; }
        protected int NumParallelTasks { get; set; } = 5;
        public int NumPackagesToTake { get; set; } = 20;
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
            List<string> queryStrings = await GetAllQueryUrisAsync(httpClient, query, NumPackagesToTake);

            var queryStringsQueue = new BufferBlock<string>(new DataflowBlockOptions { BoundedCapacity = NumParallelTasks });
            // start producer and consumer
            var producer = Produce(queryStringsQueue, queryStrings);
            var consumer = Consume(queryStringsQueue, httpClient);

            await Task.WhenAll(producer, consumer, queryStringsQueue.Completion);
            var result = await consumer;
            List<NuGetPackage> allPackages = consumer.Result.ToList();

            return allPackages;
        }

        protected async Task Produce(BufferBlock<string> queryStringQueue, List<string>queryStrings) {

            foreach(var str in queryStrings) {
                await queryStringQueue.SendAsync<string>(str);
            }

            queryStringQueue.Complete();
        }

        protected async Task<IEnumerable<NuGetPackage>> Consume(BufferBlock<string>queryStringsQueue, HttpClient httpClient) {
            var resultList = new List<NuGetPackage>();

            while(await queryStringsQueue.OutputAvailableAsync()) {
                var result = await ExecuteQueryAsync(httpClient, await queryStringsQueue.ReceiveAsync());
                resultList.AddRange(result.Packages);
            }

            return resultList;
        }

        /// <summary>
        /// Will construct a list of strings that should be used to query and get all package results.
        /// </summary>
        /// <param name="nugetInitialSearchResult">Initial nuget query result</param>
        /// <param name="numPackagesToTake">num of packages to take on each query</param>
        /// <returns></returns>
        protected internal async Task<List<string>> GetAllQueryUrisAsync(HttpClient httpClient, string query, int numPackagesToTake) {
            Debug.Assert(httpClient != null);
            Debug.Assert(!string.IsNullOrEmpty(query));
            Debug.Assert(numPackagesToTake > 0);

            var queryStrings = new List<string>();
            var initialResult = await ExecuteQueryAsync(httpClient, $"?q={query}&take=1");
            int numResultsSoFar = 0;
            do {
                queryStrings.Add($"?q={query}&take={numPackagesToTake}&skip={numResultsSoFar}");
                numResultsSoFar += numPackagesToTake;
                
                // update numResultsSoFar
            } while (numResultsSoFar < initialResult.TotalHits);

            return queryStrings;
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
        public string GetDownloadUrlFor(NuGetPackage pkg) {
            Debug.Assert(pkg != null);

            var downloadBarUrl = "https://api.nuget.org/v3-flatcontainer";
            var lowerVersion = pkg.Version.ToString().ToLowerInvariant();
            var lowerId = pkg.Id.ToLowerInvariant();

            // GET {@id}/{LOWER_ID}/{LOWER_VERSION}/{LOWER_ID}.{LOWER_VERSION}.nupkg
            var url = $"{downloadBarUrl}/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
            return url;
        }
    }
}

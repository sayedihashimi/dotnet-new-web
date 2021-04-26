using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries,List<string>specificPackagesToInclude, List<string> packagesToIgnore);
        Task<string> GetLatestVersionForAsync(HttpClient httpClient, string id);
    }

    // TODO: rename class and interface
    public class NuGetHelper : INuGetHelper {
        public NuGetHelper(IRemoteFile remoteFile) {
            RemoteFile = remoteFile;
        }

        protected IRemoteFile RemoteFile { get; set; }
        protected int NumParallelTasks { get; set; } = 5;
        public int NumPackagesToTake { get; set; } = 100;
        // TODO: set this instead of hard-coding to true
        public bool EnableVerbose { get; set; } = true;
        public async Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string[] queries, List<string> specificPackagesToInclude, List<string> packagesToIgnore) {
            Debug.Assert(httpClient != null);
            Debug.Assert(queries != null && queries.Length > 0);

            if (packagesToIgnore == null) {
                packagesToIgnore = new List<string>();
            }
            List<string> packagesToIgnoreNormalized = new List<string>();
            foreach (string pkg in packagesToIgnore) {
                packagesToIgnoreNormalized.Add(Normalize(pkg));
            }
            Console.WriteLine($"verbose: num packages to ignore '{packagesToIgnoreNormalized.Count}'");
            Dictionary<string, NuGetPackage> foundPkgsMap = new Dictionary<string, NuGetPackage>();
            foreach (string query in queries) {
                try {
                    var found = await QueryNuGetAsync(httpClient, query);
                    if (found == null || found.Count <= 0) {
                        continue;
                    }

                    foreach (var pkg in found) {
                        var key = Normalize(pkg.Id);
                        if (!packagesToIgnoreNormalized.Contains(key) && !foundPkgsMap.ContainsKey(key)) {
                            foundPkgsMap.Add(key, pkg);
                            WriteVerbose($"found package '{pkg.Id}'");
                        }
                    }
                }
                catch (Exception ex) {
                    // log error to console but continue
                    // TODO: should we be continuing?
                    Console.WriteLine(ex.ToString());
                }
            }

            // add specificPackagesToInclude now
            if(specificPackagesToInclude != null && specificPackagesToInclude.Count> 0) {
                var pkgsToAdd = new List<NuGetPackage>();

                foreach(var pkg in specificPackagesToInclude) {
                    var normalizedPackageId = Normalize(pkg);
                    
                    // if the package is in the exclude list then skip it
                    if (packagesToIgnoreNormalized.Contains(normalizedPackageId) || foundPkgsMap.ContainsKey(normalizedPackageId)) {
                        continue;
                    }

                    var query = $"?q={normalizedPackageId}&take=1&prerelease=true";
                    // ExecuteQueryAsync(httpClient, $"?q={query}&take=1&prerelease=true")
                    var foundpackage = await ExecuteQueryAsync(httpClient, query);
                    if(foundpackage != null && foundpackage.Packages.Length == 1) {
                        pkgsToAdd.Add(foundpackage.Packages[0]);

                        foundPkgsMap.Add(normalizedPackageId, foundpackage.Packages[0]);
                        WriteVerbose($"found package '{foundpackage.Packages[0].Id}'");
                    }
                    else if(foundpackage?.Packages.Length > 1) {
                        WriteVerbose($"found more than one pkg for '{pkg}', skipping results");
                    }
                }
            }

            return foundPkgsMap.Values.ToList<NuGetPackage>();
        }
        private void WriteVerbose(string str) {
            if (EnableVerbose) {
                Console.Write("verbose: ");
                Console.WriteLine(str);
            }
        }
        private string Normalize(string keyStr) {
            return keyStr.ToLowerInvariant();
        }
        public async Task<List<NuGetPackage>> QueryNuGetAsync(HttpClient httpClient, string query) {
            List<string> queryStrings = await GetAllQueryUrisAsync(httpClient, query, NumPackagesToTake);
            // https://blog.stephencleary.com/2012/11/async-producerconsumer-queue-using.html
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

            List<string> processed = new List<string>();

            while(await queryStringsQueue.OutputAvailableAsync()) {
                var query = await queryStringsQueue.ReceiveAsync();
                if (!processed.Contains(query)) {
                    var result = await ExecuteQueryAsync(httpClient, query);
                    resultList.AddRange(result.Packages);
                    
                    processed.Add(query);
                }
                else {
                    Console.WriteLine("skipping");
                }
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
            var initialResult = await ExecuteQueryAsync(httpClient, $"?q={query}&take=1&prerelease=true");
            int numResultsSoFar = 0;
            do {
                queryStrings.Add($"?q={query}&prerelease=true&take={numPackagesToTake}&skip={numResultsSoFar}");
                numResultsSoFar += numPackagesToTake;
                
                // update numResultsSoFar
            } while (numResultsSoFar < initialResult.TotalHits);

            return queryStrings;
        }

        protected async Task<NuGetSearchApiResult> ExecuteQueryAsync(HttpClient httpClient, string query, int numRetries = 3) {
            Debug.Assert(httpClient != null);
            Debug.Assert(!string.IsNullOrEmpty(query));
              
            Uri queryUri = new Uri(new Uri(Strings.NuGetQueryBaseUrl), query);

            NuGetSearchApiResult result = null;
            try {
                var resultJson = await GetApiJsonResultsAsync(httpClient, queryUri, numRetries);
                result = JsonConvert.DeserializeObject<NuGetSearchApiResult>(resultJson);
            }
            catch(Exception ex) {
                Console.WriteLine($"warning: {ex}");
            }

            if(result == null) {
                throw new NuGetQueryException($"Unable to query nuget for uri: {queryUri}");
            }

            return result;
        }

        private async Task<string> GetApiJsonResultsAsync(HttpClient httpClient, Uri uri, int numRetries) {
            Debug.Assert(httpClient != null);
            Debug.Assert(uri != null);
            Debug.Assert(numRetries >= 0);

            int numRuns = 0;

            do {
                Console.WriteLine($"get api result for '{uri}'");
                string jsonResult = null;
                try {
                    jsonResult  = await httpClient.GetStringAsync(uri);
                    return jsonResult;
                }
                catch(Exception ex) {
                    throw new NuGetQueryException($"numRetries ({numRetries}) exceed without getting a result for '{uri}'.\n{ex}");
                }

                numRuns++;
            } while (numRuns <= numRetries);

            throw new NuGetQueryException($"numRetries ({numRetries}) exceed without getting a result for '{uri}'");
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

        public NuGetPackage GetNuGetPackageById(string id) {
            Debug.Assert(!string.IsNullOrEmpty(id));

            // https://api.nuget.org/v3/registration3/{ID}/index.json


            throw new NotImplementedException();
        }

        // TODO: Come back to this
        public async Task<string> GetLatestVersionForAsync(HttpClient httpClient, string id) {
            Debug.Assert(httpClient != null);
            Debug.Assert(!string.IsNullOrEmpty(id));
            // https://api.nuget.org/v3-flatcontainer/{ID}/index.json
            var normalizedId = Normalize(id);
            var packageVersionUri = new Uri($"{Strings.NuGetPackageInfoBaseUrl}/{normalizedId}/index.json");

            // get the result, it is a list of strings
            var versionsJson = await httpClient.GetStringAsync(packageVersionUri);
            var versionsJobj = JObject.Parse(versionsJson);
            
            var retValue = versionsJobj["versions"].Values<string>().ToList().Last();

            return retValue;
        }
    }
}

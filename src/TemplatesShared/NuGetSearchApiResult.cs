using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared
{
    public class NuGetSearchApiResult
    {
        [JsonProperty("@context")]
        public NuGetApiContext Context { get; set; }
        [JsonProperty("totalHits")]
        public int TotalHits { get; set; }
        [JsonProperty("data")]
        public NuGetPackage[] Packages { get; set; }
    }
    public class NuGetApiContext
    {
        [JsonProperty("@Vocab")]
        public string Vocab { get; set; }
        [JsonProperty("@Base")]
        public string Base { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesShared
{
    public class NuGetSearchApiResult
    {
        [JsonProperty("@Context")]
        public NuGetApiContext Context { get; set; }
        public NuGetPackage[] Data { get; set; }
    }
    public class NuGetApiContext
    {
        [JsonProperty("@Vocab")]
        public string Vocab { get; set; }
        [JsonProperty("@Base")]
        public string Base { get; set; }
    }
}

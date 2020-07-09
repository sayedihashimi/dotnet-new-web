using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TemplatesShared;

namespace TemplatesConsole
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            string baseurl = "https://azuresearch-usnc.nuget.org/query";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseurl);
            var result = await client.GetStringAsync(@"?q=sidewaffle&take=50&prerelease=true");

            try
            {
                var list = JsonConvert.DeserializeObject<NuGetSearchApiResult>(result);
            }
            catch(Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
            }
            // var result = JsonConvert.DeserializeObject<List<TemplatePack>>(text);



            Console.WriteLine("Hello World!");
        }
    }
}

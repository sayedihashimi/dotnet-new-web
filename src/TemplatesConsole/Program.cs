using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.DragonFruit;
using System.CommandLine.Invocation;
using System.Net.Http;
using TemplatesShared;

namespace TemplatesConsole {
    class Program {
        /// <summary>
        /// .NET Core tool
        /// </summary>
        /// <param name="argument">Command to execute</param>
        /// <param name="args">Additional arguments</param>
        static void Main(string argument,string[] args) {
            var parent = new RootCommand("parent");

            var helloCommand = new Command("hello");
            helloCommand.Handler = CommandHandler.Create(() => {
                SayHello();
            });
            var queryCommand = new Command("query");
            queryCommand.Handler = CommandHandler.Create(async (string somenamehere) => {
                await SampleQuery(somenamehere);
            });

            

            // parent.Add(helloCommand);
            parent.Add(new HelloCommand("hello", "will say hello"));
            parent.Add(queryCommand);

            Command cmdToRun = null;

            cmdToRun = (argument) switch
            {
                "hello" => helloCommand,
                "query" => queryCommand,
                _ => (Command)null
            };

            if(cmdToRun != null) {
                cmdToRun.Invoke(args);
            }
        }

        public class HelloCommand : Command {
            protected Option<string> optionName { get; set; } = new Option<string>("--your-name", "your name here");
            /// <summary>
            /// Your name here
            /// </summary>
            public string YourName { get; set; }

            public Argument<string> Arg1 { get; } = new Argument<string>("--name", "your name here");

#nullable enable
            public HelloCommand(string name, string? description = null) : base(name,description) {
                this.AddOption(optionName);
                AddArgument(Arg1);
                this.Handler = CommandHandler.Create(() => {
                    
                    System.Console.WriteLine($"Hello ");
                });
            }
#nullable disable
        }


        static void SayHello() {
            System.Console.WriteLine("hello");
        }
        static async System.Threading.Tasks.Task SampleQuery(string name) {
            string baseurl = "https://azuresearch-usnc.nuget.org/query";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseurl);
            var result = await client.GetStringAsync(@"?q=sidewaffle&take=50&prerelease=true");

            NuGetSearchApiResult searchResult = null;
            try {
                searchResult = JsonConvert.DeserializeObject<NuGetSearchApiResult>(result);
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex.ToString());
            }

            Console.WriteLine($"query executed, num results: {searchResult.Packages.Length}");
        }


    }
}

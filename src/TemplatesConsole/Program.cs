using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TemplatesShared;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace TemplatesConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var parent = new RootCommand();
            var helloCommand = new HelloCommand("hello","hello command");
            parent.Add(helloCommand);

            parent.InvokeAsync(args).Wait();
        }

        private static async System.Threading.Tasks.Task DoQueryAsync() {
            string baseurl = "https://azuresearch-usnc.nuget.org/query";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseurl);
            var result = await client.GetStringAsync(@"?q=sidewaffle&take=50&prerelease=true");

            try {
                var nugetResult = JsonConvert.DeserializeObject<NuGetSearchApiResult>(result);
                Console.WriteLine($"Num packages: {nugetResult.Packages.Length}");
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex.ToString());
            }
        }
    }

    public class HelloCommand : Command {
        //protected Option<string> optionName { get; set; } = new Option<string>("--your-name", "your name here");
        /// <summary>
        /// Your name here
        /// </summary>
        public string YourName { get; set; }

        // public Argument Arg1 { get; } = new Argument() { Name="Argument", Description = "your name here" };
        
        public string Argument { get; set; }

#nullable enable
        public HelloCommand(string name, string? description = null) : base(name, description) {

            //AddArgument(new System.CommandLine.Argument {
            //    Name = "Argument",
            //    Description = "your name here"
            //});
            AddArgument(new System.CommandLine.Argument("yourName"));
            
            AddOption(new Option<string>("--argument", "your name here"));

            //this.AddOption(optionName);
            // AddArgument(Arg1);

            //this.Handler = CommandHandler.Create(() => {
            //    System.Console.WriteLine($"Hello {Argument}|{YourName}");
            //});


            this.Handler = CommandHandler.Create<string,string>((string argument, string yourName) => {
                System.Console.WriteLine($"Hello {argument}|{Argument}|{YourName}|{yourName}");
            });




            
        }
#nullable disable
    }
}

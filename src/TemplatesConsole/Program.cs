using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using TemplatesShared;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using McMaster.Extensions.CommandLineUtils;

namespace TemplatesConsole {
    class Program {
        static int Main(string[] args) {
            var app = new CommandLineApplication {
                Name = "templatereport",
                Description = "template report tool"
            };

            app.HelpOption(inherited: true);

            var helloCommand = new MyHelloCommand2();
            var barCommand = new MyBarCommad();

            app.Command(helloCommand.Name, helloCommand.Setup);
            app.Command(barCommand.Name, barCommand.Setup);

            app.OnExecute(() => {
                Console.WriteLine("Specify a subcommand to execute\n");
                app.ShowHelp();
                return -1;
            });

            return app.Execute(args);
        }

        static async System.Threading.Tasks.Task QueryOld(string[] args) {
            string baseurl = "https://azuresearch-usnc.nuget.org/query";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseurl);
            var result = await client.GetStringAsync(@"?q=sidewaffle&take=50&prerelease=true");

            try {
                var list = JsonConvert.DeserializeObject<NuGetSearchApiResult>(result);
            }
            catch (Exception ex) {
                System.Console.WriteLine(ex.ToString());
            }
            // var result = JsonConvert.DeserializeObject<List<TemplatePack>>(text);



            Console.WriteLine("Hello World!");
        }
    }
}

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
        static void Main(string[] args) {
            new Program(args).Run();
        }

        private string[] _args;
        private ServiceCollection _services = null;
        private ServiceProvider _serviceProvider = null;

        private Program(string[] args) {
            _args = args;
            RegisterServices();
        }

        private void Run() {
            //using var app = new CommandLineApplication<Program> {
            //    Name = "templatereport",
            //    UsePagerForHelpText = false
            //};

            using var app = new CommandLineApplication<Program>();

            app.ExtendedHelpText = @"
            Remarks:
              Typical hello app.
            ";

            app.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection();

            app.HelpOption(true);

            // add sub commands here
            app.AddSubcommand(new HelloCommand<Program>());

            app.Execute(_args);
        }

        private void OnExecute() {
            Console.WriteLine("OnExecute");
        }

        private void RegisterServices() {
            _services = new ServiceCollection();

        }

        private TType GetFromServices<TType>() {
            Debug.Assert(_serviceProvider != null);
            return _serviceProvider.GetRequiredService<TType>();
        }

        static async System.Threading.Tasks.Task MainOld(string[] args) {
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

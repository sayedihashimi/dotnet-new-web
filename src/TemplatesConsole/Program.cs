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
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace TemplatesConsole {
    class Program {
        static ServiceCollection _services = null;
        static ServiceProvider _serviceProvider = null;
        static int Main(string[] args) {
            RegisterServices();

            var app = new CommandLineApplication {
                Name = "templatereport",
                Description = "template report tool"
            };

            app.HelpOption(inherited: true);

            var queryCommand = new QueryCommand(GetFromServices<INuGetHelper>());
            var reportCommand = new ReportCommand(
                GetFromServices<HttpClient>(),
                GetFromServices<INuGetHelper>(),
                GetFromServices<IRemoteFile>(),
                GetFromServices<INuGetPackageDownloader>());

            app.Command(queryCommand.Name, queryCommand.Setup);
            app.Command(reportCommand.Name, reportCommand.Setup);

            app.OnExecute(() => {
                Console.WriteLine("Specify a subcommand to execute\n");
                app.ShowHelp();
                return -1;
            });

            return app.Execute(args);
        }

        private static void RegisterServices() {
            _services = new ServiceCollection();
            _serviceProvider = _services.AddSingleton<INuGetHelper, NuGetHelper>()
                                        .AddSingleton<IRemoteFile, RemoteFile>()
                                        .AddSingleton<HttpClient>()
                                        .AddSingleton<INuGetPackageDownloader,NuGetPackageDownloader>()
                                .BuildServiceProvider();
        }

        private static TType GetFromServices<TType>() {
            return _serviceProvider.GetRequiredService<TType>();
        }
    }
}

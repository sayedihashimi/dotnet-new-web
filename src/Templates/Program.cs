using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TemplatesShared;

namespace Templates {
    class Program {
        public static Task<int> Main(string[] args) {
            return new TemplatesProgram().Execute(args);
        }
    }

    public class TemplatesProgram {
        private Parser _parser;
        protected ServiceCollection _services = null;
        protected ServiceProvider _serviceProvider = null;

        public TemplatesProgram() {
            RegisterServices();
        }

        public Task<int> Execute(string[] args) {
            _parser = new CommandLineBuilder()
                .AddCommand(
                new SearchCommand(
                    GetFromServices<IReporter>(),
                    GetFromServices<ITemplateReportLocator>(),
                    GetFromServices<ITemplateSearcher>(),
                    GetFromServices<ITemplateInstaller>())
                .CreateCommand())
                .UseDefaults()
                .Build();

            return _parser.InvokeAsync(args);
        }

        private void RegisterServices() {
            _services = new ServiceCollection();
            _serviceProvider = _services
                                .AddSingleton<IReporter, Reporter>()
                                .AddSingleton<ITemplateReportLocator, TemplateReportLocator>()
                                .AddSingleton<ITemplateSearcher, TemplateSearcher>()
                                .AddSingleton<ITemplateInstaller, TemplateInstaller>()
                                .BuildServiceProvider();
        }

        private TType GetFromServices<TType>() {
            return _serviceProvider.GetRequiredService<TType>();
        }
    }
}

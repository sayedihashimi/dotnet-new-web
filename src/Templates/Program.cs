using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Templates {
    class Program {
        public static Task<int> Main(string[] args) {
            var parser = new CommandLineBuilder()
                            .AddCommand(SearchCommand())
                            .UseDefaults()
                            .Build();

            return parser.InvokeAsync(args);
        }

        private static Command SearchCommand() =>
            new Command(name: "hello", description: "hello command") {
                CommandHandler.Create<string>( (searchTerm) => {
                    Console.WriteLine($"hello {searchTerm}"); }
                ),
                ArgSearchTerm()
            };

        private static Argument ArgSearchTerm() =>
            new Argument<string>(
                name: "search-term") {
                Description = "search term"
            };
    }
}

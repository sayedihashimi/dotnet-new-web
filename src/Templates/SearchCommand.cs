using System;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Templates {
    public class SearchCommand : CommandBase {

        public override Command CreateCommand() =>
            new Command(name: "search", description: "search for templates") {
                CommandHandler.Create<string>( (searchTerm) => {
                    Console.WriteLine($"hello {searchTerm}");
                }),
                ArgSearchTerm()
            };

        protected Argument ArgSearchTerm() =>
            new Argument<string>(
                name: "search-term") {
                Description = "search term"
            };
    }
}

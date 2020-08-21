using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace Templates {
    public class SearchCommand : CommandBase {
        private IReporter _reporter;
        public SearchCommand(IReporter reporter) {
            Debug.Assert(reporter != null);
            _reporter = reporter;
        }
        public override Command CreateCommand() =>
            new Command(name: "search", description: "search for templates") {
                CommandHandler.Create<string>( (searchTerm) => {
                    _reporter.WriteLine($"hello {searchTerm}");
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

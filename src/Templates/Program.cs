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
            return new TemplatesProgram().Execute(args);
        }
    }

    public class TemplatesProgram {
        private Parser _parser;


        public Task<int> Execute(string[] args) {
            _parser = new CommandLineBuilder()
                        .AddCommand(new SearchCommand().CreateCommand())
                        .UseDefaults()
                        .Build();

            return _parser.InvokeAsync(args);
        }
    }
}

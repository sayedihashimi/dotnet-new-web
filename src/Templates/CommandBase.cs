using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace Templates {
    public interface ICommand {
        Command CreateCommand();
    }
    public abstract class CommandBase : ICommand {
        public abstract Command CreateCommand();

        protected Option OptionVerbose() =>
            new Option(alias: "--verbose", description: "enables verbose output")
            {
                Argument = new Argument<bool>(name: "verbose")
            };
        public bool EnableVerbose { get; set; }
    }
}

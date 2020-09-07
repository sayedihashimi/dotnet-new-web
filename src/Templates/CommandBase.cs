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
    }
}

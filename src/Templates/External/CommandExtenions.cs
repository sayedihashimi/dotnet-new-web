using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

namespace Templates {
    // from: https://github.com/dotnet/diagnostics/blob/master/src/Tools/Common/CommandExtensions.cs
    public static class CommandExtenions {
        /// <summary>
        /// Allows the command handler to be included in the collection initializer.
        /// </summary>
        public static void Add(this Command command, ICommandHandler handler) {
            command.Handler = handler;
        }
    }
}

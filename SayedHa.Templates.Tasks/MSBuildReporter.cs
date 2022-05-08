using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplatesShared;

namespace SayedHa.Templates.Tasks {
    public class MSBuildReporter : IReporter {
        private TaskLoggingHelper _loggingHelper;

        public MSBuildReporter(TaskLoggingHelper loggingHelper) {
            Debug.Assert(loggingHelper != null);
            _loggingHelper = loggingHelper;
        }
        public bool EnableVerbose { get; set; }

        public void Write(string output) {
            throw new NotImplementedException();
        }

        public void WriteLine(string output) {
            _loggingHelper.LogMessage(output);
        }

        public void WriteLine() {
            throw new NotImplementedException();
        }

        public void WriteLine(string output, string prefix) {
            _loggingHelper.LogMessage($"{prefix}{output}");
        }

        public void WriteVerbose(string output) {
            _loggingHelper.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, output);
        }

        public void WriteVerboseLine(string output, bool includePrefix = true) {
            _loggingHelper.LogMessage(Microsoft.Build.Framework.MessageImportance.Low, output);
        }

        public void WriteVerboseLine() {
            throw new NotImplementedException();
        }
    }
}

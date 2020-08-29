using System;
using System.Collections.Generic;
using System.Text;

namespace Templates {
    public interface IReporter {
        bool EnableVerbose { get; set; }
        void Write(string output);
        void WriteLine(string output);
        void WriteLine();
        void WriteVerbose(string output);
        void WriteVerboseLine(string output, bool includePrefix = true);
    }

    public class Reporter : IReporter {
        public bool EnableVerbose { get; set; }

        public void WriteLine() {
            Console.WriteLine();
        }
        public void WriteLine(string output) {
            Console.WriteLine(output);
        }
        public void Write(string output) {
            Console.Write(output);
        }
        public void WriteVerboseLine(string output, bool includePrefix = true) {
            if (EnableVerbose) {
                if (includePrefix) {
                    Write("verbose: ");
                }
                Write(output);
                Write(Environment.NewLine);
            }
        }
        public void WriteVerbose(string output) {
            if (EnableVerbose) {
                Write(output);
            }
        }
    }
}

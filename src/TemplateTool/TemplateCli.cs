using System;
using System.Reflection;

namespace TemplateTool
{
    public class TemplateCli
    {
        public TemplateCli(string[]cliArgs)
        {
            _cliArgs = cliArgs;
        }

        private string[] _cliArgs;

        public void Run()
        {
            if (!CheckArgs())
            {
                DisplayUsage();
                return;
            }

            System.Console.WriteLine(string.Join(' ', _cliArgs));
        }

        private void DisplayUsage()
        {
            var verString = GetVersion();
            var usage = $@"{Strings.ToolName} v{verString}
-------------
Usage:
{Strings.ToolName} <search-term>";
        }

        private string GetVersion()
        {
            return Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
        }

        private bool CheckArgs()
        {
            if(_cliArgs == null || _cliArgs.Length <=0)
            {
                return false;
            }
            return true;
        }


    }
}

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace TemplatesConsole {
    public abstract class TemplateCommand {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool EnableVerboseOption { get; protected set; } = true;
        /// <summary>
        /// If Verbose is true then this will have a value after Setup is called,
        /// otherwise it will remain null
        /// </summary>
        protected CommandOption<bool> OptionVerbose { get; set; }

        public Func<int> OnExecute { get; protected set; }
        public virtual void Setup(CommandLineApplication command) {
            ValidateSetup();

            command.Name = Name;
            command.Description = Description;
            if (EnableVerboseOption) {
                OptionVerbose = command.Option<bool>(
                    "--verbose",
                    "enable verbose output",
                    CommandOptionType.NoValue);
            }


            command.OnExecute(() => { return this.OnExecute(); });
        }

        protected virtual void ValidateSetup() {
            if (string.IsNullOrWhiteSpace(Name)) {
                throw new TemplateCommandSetupException("name is empty");
            }
            if (string.IsNullOrWhiteSpace(Description)) {
                throw new TemplateCommandSetupException("description is empty");
            }
        }
    }

    public class MyHelloCommand2 : TemplateCommand {
        public MyHelloCommand2() : base() {
            Name = "hello";
            Description = "hello command";
        }

        public override void Setup(CommandLineApplication command) {
            base.Setup(command);

            var argName = command.Argument("name", "your name");
            var optionLastName = command.Option<string>("-ln|--last-name", "your last name", CommandOptionType.SingleValue);
            OnExecute = () => {
                Console.WriteLine($"hello {argName.Value} {optionLastName.Value()}");
                return 1;
            };
        }
    }

    public class MyBarCommad : TemplateCommand {
        public MyBarCommad() : base() {
            Name = "bar";
            Description = "bar command here";
            OnExecute = () => {
                Console.WriteLine("hello from bar");
                return 1;
            };
        }
    }
}

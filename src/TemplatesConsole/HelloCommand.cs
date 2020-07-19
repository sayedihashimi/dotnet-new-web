using McMaster.Extensions.CommandLineUtils;
using SayedHa.Commands.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesConsole {
    public class HelloCommand<T> : BaseCommandLineApplication<T> where T:class{
        public HelloCommand() : base(
            "hello",
            "helloCommand",
            "Says hello") {

            this.OnExecute(() => {
                Console.WriteLine("hello");
                return 1;
            });

        }
    }

    public class FooCommand<T> : CommandLineApplication<T> where T:class {
        T foo { get; set; }
        public FooCommand() : base(){
            }


        public void Foo() {
            var f = new CommandLineApplication<String>();
        }
    }

}

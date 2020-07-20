using McMaster.Extensions.CommandLineUtils;
using SayedHa.Commands.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace TemplatesConsole {
    public class MyHelloCommand : BaseCommandLineApplication {
        public MyHelloCommand() : base(
            "hello",
            "helloCommand",
            "Says hello") {

            this.OnExecute(() => {
                Console.WriteLine("hello");
                return 1;
            });

        }
    }

    public class FooCommand : BaseCommandLineApplication {
        public FooCommand() : base(
            "foo",
            "fooCommand",
            "Foo command here") {


            this.OnExecute(() => {
                Console.WriteLine("foo says hello");
            });
        }
    }

}

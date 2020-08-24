using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace TemplatesShared {
    public enum PromptType {
        TrueFalse   = 1,
        FreeText    = 2,
        PickOne     = 3,
        PickMany    = 4
    }

    public class Prompt<T> :Prompt{
        public T ParsedValue() {
            return (T)Result;
        }
    }

    public class TrueFalsePrompt : Prompt<bool> {
        public TrueFalsePrompt(string text) {
            Text = text;
            PromptType = PromptType.TrueFalse;
        }
    }
    public class FreeTextPrompt : Prompt<string> {
        public FreeTextPrompt(string text) {
            Text = text;
            PromptType = PromptType.FreeText;
        }
    }
    public abstract class OptionsPrompt : Prompt<string> {
        public OptionsPrompt(string text, List<string> userOptions) {
            Text = text;
            UserOptions = userOptions;
        }
        public List<string> UserOptions { get; private set; }
    }
    public class PickOnePrompt : OptionsPrompt {
        public PickOnePrompt(string text,List<string>userOptions) : base(text,userOptions) {
            PromptType = PromptType.PickOne;
        }
    }
    public class PickManyPrompt : OptionsPrompt {
        public PickManyPrompt(string text, List<string> userOptions):base(text,userOptions) {
            PromptType = PromptType.PickMany;
        }
    }

    public class Prompt {
        public PromptType PromptType { get; protected set; }
        /// <summary>
        /// Text that will be shown to the user
        /// </summary>
        public string Text { get; protected set; }
        /// <summary>
        /// Result will be one of:
        ///  - boolean
        ///  - string
        ///  - list of string
        /// </summary>
        public object Result { get; set; }
    }

    public interface IPromptInvoker {
        Prompt GetPromptResult(Prompt prompt);
        List<Prompt> GetPromptResults(List<Prompt> prompts);
    }

    public class PromptInvoker : IPromptInvoker {
        private IConsoleWrapper _console;
        public PromptInvoker(IConsoleWrapper consoleWrapper) {
            Debug.Assert(consoleWrapper != null);
            _console = consoleWrapper;
        }
        public List<Prompt> GetPromptResults(List<Prompt> prompts) {
            foreach(var prompt in prompts) {
                GetPromptResult(prompt);
            }
            return prompts;
        }
        public Prompt GetPromptResult(Prompt prompt) {
            Debug.Assert(prompt != null);
            
            _console.WriteLine();
            _console.Write(prompt.Text);

            switch (prompt.PromptType) {
                case PromptType.TrueFalse:
                    _console.Write(" - (y/n)");
                    _console.WriteLine();
                    _console.Write(">>");
                    prompt.Result = ConvertToBool(_console.ReadKey().Key);
                    break;
                case PromptType.FreeText:
                    _console.Write(" - (press enter after response)");
                    _console.WriteLine();
                    _console.Write(">>");
                    prompt.Result = _console.ReadLine();
                    break;
                case PromptType.PickOne:
                    break;
                case PromptType.PickMany:
                    break;
                default:
                    throw new NotImplementedException();
            }

            return prompt;
        }
        //protected bool? ConvertToBool1(ConsoleKey key) {
        //    bool? result = false;


        //    result = key switch
        //    {
        //        ConsoleKey.Y => true,
        //        ConsoleKey.N => false,
        //        _ => null
        //    };

        //    return result;
        //}

        protected bool? ConvertToBool(ConsoleKey key) =>
            key switch
            {
                ConsoleKey.Y => true,
                ConsoleKey.N => false,
                _ => null
            };
    }
}

using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
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
        public OptionsPrompt(string text, List<UserOptions> userOptions) {
            Text = text;
            UserOptions = userOptions;
        }
        public List<UserOptions> UserOptions { get; private set; }
    }
    public class PickOnePrompt : OptionsPrompt {
        public PickOnePrompt(string text,List<UserOptions>userOptions) : base(text,userOptions) {
            PromptType = PromptType.PickOne;
        }
    }
    public class PickManyPrompt : OptionsPrompt {
        public PickManyPrompt(string text, List<UserOptions> userOptions):base(text,userOptions) {
            PromptType = PromptType.PickMany;
        }
    }
    public class UserOptions {
        public string Text { get; set; }
        // TODO: Not used currently
        public bool IsRequired { get; set; }

        public static List<UserOptions> ConvertToOptions(List<string> optionsText) {
            var result = new List<UserOptions>();
            foreach(var ot in optionsText) {
                result.Add(new UserOptions { Text = ot });
            }
            return result;
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
                    GetPromptResult(prompt as TrueFalsePrompt);
                    break;
                case PromptType.FreeText:
                    GetPromptResult(prompt as FreeTextPrompt);
                    break;
                case PromptType.PickOne:
                    GetPromptResult(prompt as PickOnePrompt);
                    break;
                case PromptType.PickMany:
                    break;
                default:
                    throw new NotImplementedException();
            }

            return prompt;
        }

        protected TrueFalsePrompt GetPromptResult(TrueFalsePrompt prompt) {
            _console.Write(" - (y/n)");
            _console.WriteLine();
            _console.Write(">>");
            prompt.Result = ConvertToBool(_console.ReadKey().Key);
            return prompt;
        }

        protected FreeTextPrompt GetPromptResult(FreeTextPrompt prompt) {
            _console.Write(" - (press enter after response)");
            _console.WriteLine();
            _console.Write(">>");
            prompt.Result = _console.ReadLine();
            return prompt;
        }

        protected PickOnePrompt GetPromptResult(PickOnePrompt prompt) {
            _console.WriteLine();
            _console.IncreaseIndent();
            var promptCursorMap = new Dictionary<UserOptions, (int CursorLeft, int CursorRight)>();
            var cursorList = new List<(int CursorLeft, int CursorTop)>();
            foreach(var uo in prompt.UserOptions) {
                _console.WriteIndent();
                _console.Write("[");
                cursorList.Add(_console.GetCursorPosition());
                _console.Write(" ");
                // capture cursor location now
                promptCursorMap.Add(uo, _console.GetCursorPosition());                
                _console.Write("] ");
                _console.WriteLine(uo.Text);                
            }
            var endCursorLocation = _console.GetCursorPosition();
            // put cursor at first location
            _console.SetCursorPosition(cursorList[0].CursorLeft,cursorList[0].CursorTop);

            int currentIndex = 0;
            // handle key events
            var continueLoop = true;
            while (continueLoop) {
                var key = _console.ReadKey(true);

                switch (key.Key) {
                    case ConsoleKey.UpArrow:
                        currentIndex--;
                        _console.SetCursorPosition(cursorList[currentIndex].CursorLeft, cursorList[currentIndex].CursorTop);
                        break;
                    case ConsoleKey.DownArrow:
                        currentIndex++;
                        _console.SetCursorPosition(cursorList[currentIndex].CursorLeft, cursorList[currentIndex].CursorTop);
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.X:
                        _console.Write("X");
                        break;
                    case ConsoleKey.Q:
                        continueLoop = false;
                        break;
                    default:
                        continueLoop = false;
                        break;
                }
            }



            // wait for the user to select an option
            _console.ReadLine();


            // reset cursor location 
            _console.SetCursorPosition(endCursorLocation.Left,endCursorLocation.Top);
            _console.DecreaseIndent();



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

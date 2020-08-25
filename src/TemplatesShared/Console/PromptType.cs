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
        public OptionsPrompt(string text, List<UserOption> userOptions) {
            Text = text;
            UserOptions = userOptions;
        }
        public List<UserOption> UserOptions { get; private set; }
    }
    public class PickOnePrompt : OptionsPrompt {
        public PickOnePrompt(string text,List<UserOption>userOptions) : base(text,userOptions) {
            PromptType = PromptType.PickOne;
        }
    }
    public class PickManyPrompt : OptionsPrompt {
        public PickManyPrompt(string text, List<UserOption> userOptions):base(text,userOptions) {
            PromptType = PromptType.PickMany;
        }
    }
    public class UserOption {
        public string Text { get; set; }
        public bool IsSelected { get; set; }
        // TODO: IsRequired Not used currently
        public bool IsRequired { get; set; }
        
        /// <summary>
        /// Will toggle the value of IsSelected and then return the new value
        /// </summary>
        /// <returns>New value for IsSelected</returns>
        public bool ToggleIsSelected() {
            IsSelected = !IsSelected;
            return IsSelected;
        }


        public static List<UserOption> ConvertToOptions(List<string> optionsText) {
            var result = new List<UserOption>();
            foreach(var ot in optionsText) {
                result.Add(new UserOption { Text = ot });
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

        protected OptionsPrompt GetPromptResult(OptionsPrompt prompt) {
            _console.WriteLine();
            _console.IncreaseIndent();
            var promptCursorMap = new Dictionary<(int CursorLeft, int CursorRight), UserOption>();
            var cursorList = new List<(int CursorLeft, int CursorTop)>();
            foreach(var uo in prompt.UserOptions) {
                _console.WriteIndent();
                _console.Write("[");
                // capture cursor location now
                cursorList.Add(_console.GetCursorPosition());                
                promptCursorMap.Add(_console.GetCursorPosition(), uo);
                _console.Write(" ");                
                _console.Write("] ");
                _console.WriteLine(uo.Text);                
            }
            var endCursorLocation = _console.GetCursorPosition();
            // put cursor at first location
            _console.SetCursorPosition(cursorList[0]);

            int currentIndex = 0;
            // handle key events
            var continueLoop = true;
            while (continueLoop) {
                var key = _console.ReadKey(true);

                switch (key.Key) {
                    case ConsoleKey.UpArrow:
                        currentIndex = currentIndex > 0 ? (--currentIndex) % cursorList.Count : cursorList.Count - 1;
                        _console.SetCursorPosition(cursorList[currentIndex]);
                        break;
                    case ConsoleKey.DownArrow:
                        currentIndex = (++currentIndex) % cursorList.Count;
                        _console.SetCursorPosition(cursorList[currentIndex]);
                        break;
                    case ConsoleKey.Spacebar:
                    case ConsoleKey.X:
                        var orgCursorPosn = _console.GetCursorPosition();
                        var selectedOption = GetPromptAtPosition(orgCursorPosn);
                        if(selectedOption != null) {
                            var isSelected = selectedOption.ToggleIsSelected();
                            var charToWrite = isSelected ? 'X' : ' ';
                            var posn = _console.GetCursorPosition();
                            _console.Write(charToWrite);
                            _console.SetCursorPosition(posn);
                        }
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                        continueLoop = false;
                        break;
                    default:
                        break;
                }
            }

            UserOption GetPromptAtPosition((int cursorLeft, int cursorTop) cursorPosition) {
                UserOption found;
                promptCursorMap.TryGetValue(cursorPosition, out found);

                return found;
            }

            // reset cursor to it's original location
            _console.SetCursorPosition(endCursorLocation);

            _console.DecreaseIndent();

            return prompt;
        }

        protected bool? ConvertToBool(ConsoleKey key) =>
            key switch
            {
                ConsoleKey.Y => true,
                ConsoleKey.N => false,
                _ => null
            };
    }
}

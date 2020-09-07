using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TemplatesShared {
    public interface IPromptInvoker {
        Prompt GetPromptResult(Prompt prompt);
        List<Prompt> GetPromptResult(List<Prompt> prompts);
    }
    public class PromptInvoker : IPromptInvoker {
        private IConsoleWrapper _console;
        public PromptInvoker(IConsoleWrapper consoleWrapper) {
            Debug.Assert(consoleWrapper != null);
            _console = consoleWrapper;
        }
        public List<Prompt> GetPromptResult(List<Prompt> prompts) {
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
                    GetPromptResult(prompt as OptionsPrompt);
                    break;
                case PromptType.PickMany:
                    GetPromptResult(prompt as OptionsPrompt);
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

            bool? result = null;
            while (result == null) {
                var keypressed = _console.ReadKey();
                switch (keypressed.Key) {
                    case ConsoleKey.Y:
                        result = true;
                        break;
                    case ConsoleKey.N:
                        result = false;
                        break;
                }
            }

            prompt.Result = result;
            return prompt;
        }

        protected FreeTextPrompt GetPromptResult(FreeTextPrompt prompt) {
            _console.Write(" - (press enter after response)");
            _console.WriteLine();
            _console.Write(">>");
            var origPosn = _console.GetCursorPosition();
            while (true) {
                prompt.Result = _console.ReadLine();
                if (!string.IsNullOrWhiteSpace((string)prompt.Result)) {
                    prompt.Result = ((string)prompt.Result).Trim();
                    break;
                }
                else {
                    _console.SetCursorPosition(origPosn);
                }
            }
            _console.SetCursorPosition(origPosn);
            return prompt;
        }

        protected OptionsPrompt GetPromptResult(OptionsPrompt prompt) {
            _console.Write(" - (press enter after selection)");
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
                        var orgCursorPosn = _console.GetCursorPosition();
                        var selectedOption = GetPromptAtPosition(orgCursorPosn);
                        if(selectedOption != null) {
                            var isSelected = selectedOption.ToggleIsSelected();
                            if (prompt.PromptType == PromptType.PickOne) {
                                foreach (var uo in prompt.UserOptions) {
                                    if (!uo.Equals(selectedOption)) {
                                        uo.IsSelected = false;
                                    }
                                }
                                RedrawOptionValues(promptCursorMap);
                            }
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
            void ResetAllOptions(List<UserOption> userOptions) {
                userOptions.ForEach((uo) => uo.IsSelected = false);
            }
            void RedrawOptionValues(Dictionary<(int CursorLeft, int CursorRight), UserOption>cursorOptionMap) {
                var originalCursorPosn = _console.GetCursorPosition();
                foreach(var posn in cursorOptionMap.Keys) {                    
                    _console.SetCursorPosition(posn);
                    var charToWrite = cursorOptionMap[posn].IsSelected ? 'X' : ' ';
                    _console.Write(charToWrite);
                }
                _console.SetCursorPosition(originalCursorPosn);
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

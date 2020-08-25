using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TemplatesShared {
    public class DirectConsoleWrapper : IConsoleWrapper {

        /// <summary>
        /// Most common usage of this class should be calling the default constructor
        /// </summary>
        public DirectConsoleWrapper():this(null) {

        }
        public DirectConsoleWrapper(IConsoleWrapper parentWrapper) {
            _parentWrapper = parentWrapper;
        }

        private readonly IConsoleWrapper _parentWrapper;
        public virtual IConsoleWrapper ParentWrapper => _parentWrapper;

        public void Clear() {
            if (_parentWrapper != null) {
                _parentWrapper.Clear();
            }
            else {
                Console.Clear();
            }
        }

        public bool IsOutputRedirected {
            get {
                if (_parentWrapper != null) {
                    return _parentWrapper.IsOutputRedirected;
                }
                else {
                    return Console.IsOutputRedirected;
                }
            }
        }

        public int Read() {
            if (_parentWrapper != null) {
                return _parentWrapper.Read();
            }
            else {
                return Console.Read();
            }
        }

        public ConsoleKeyInfo ReadKey() {
            if (_parentWrapper != null) {
                return _parentWrapper.ReadKey();
            }
            else {
                return Console.ReadKey();
            }
        }
        public ConsoleKeyInfo ReadKey(bool intercept) {
            if (_parentWrapper != null) {
                return _parentWrapper.ReadKey(intercept);
            }
            else {
                return Console.ReadKey(intercept);
            }
        }

        public string ReadLine() {
            if (_parentWrapper != null) {
                return _parentWrapper.ReadLine();
            }
            else {
                return Console.ReadLine();
            }
        }

        #region color related
        public ConsoleColor BackgroundColor {
            get {
                if (_parentWrapper != null) {
                    return _parentWrapper.BackgroundColor;
                }
                else {
                    return Console.BackgroundColor;
                }
            }
            set {
                if (_parentWrapper != null) {
                    _parentWrapper.BackgroundColor = value;
                }
                else {
                    Console.BackgroundColor = value;
                }
            }
        }
        public ConsoleColor ForegroundColor {
            get {
                if (_parentWrapper != null) {
                    return _parentWrapper.ForegroundColor;
                }
                else {
                    return Console.ForegroundColor;
                }
            }
            set {
                if (_parentWrapper != null) {
                    _parentWrapper.ForegroundColor = value;
                }
                else {
                    Console.ForegroundColor = value;
                }
            }
        }

        protected int IndentLevel { get; set; } = 1;
        public string IndentPrefix { get; set; } = "  ";
        public void IncreaseIndent() { }
        public void DecreaseIndent() { }
        public void WriteIndent() {
            if (_parentWrapper != null) {
                _parentWrapper.Write(GetIndentString());
            }
            else {
                Console.Write(GetIndentString());
            }
        }
        public string GetIndentString() {
            var sb = new StringBuilder();
            for(int i = 0; i < IndentLevel; i++) {
                sb.Append(IndentPrefix);
            }
            return sb.ToString();
        }
        #endregion

        #region write methods
        public void WriteLine() {
            if (_parentWrapper != null) {
                _parentWrapper.WriteLine();
            }
            else {
                Console.WriteLine();
            }
        }
        public void WriteLine(string value) {
            if (_parentWrapper != null) {
                _parentWrapper.WriteLine(value);
            }
            else {
                Console.WriteLine(value);
            }
        }
        public void WriteLine(object value) {
            if (_parentWrapper != null) {
                _parentWrapper.WriteLine(value);
            }
            else {
                Console.WriteLine(value);
            }
        }
        public void WriteLine(char value) {
            if (_parentWrapper != null) {
                _parentWrapper.WriteLine(value);
            }
            else {
                Console.WriteLine(value);
            }
        }

        public void Write(string value) {
            if (_parentWrapper != null) {
                _parentWrapper.Write(value);
            }
            else {
                Console.Write(value);
            }
        }
        public void Write(object value) {
            if (_parentWrapper != null) {
                _parentWrapper.Write(value);
            }
            else {
                Console.Write(value);
            }
        }
        public void Write(char value) {
            if (_parentWrapper != null) {
                _parentWrapper.Write(value);
            }
            else {
                Console.Write(value);
            }
        }
        #endregion

        #region cursor related
        public void SetCursorPosition(int left, int top) {
            if (_parentWrapper != null) {
                _parentWrapper.SetCursorPosition(left, top);
            }
            else {
                Console.SetCursorPosition(left, top);
            }
        }
        public void SetCursorPosition((int cursorLeft, int cursorTop) cursorPosition) {
            SetCursorPosition(cursorPosition.cursorLeft, cursorPosition.cursorTop);
        }
        public (int Left, int Top) GetCursorPosition() {
            if(_parentWrapper != null) {
                return _parentWrapper.GetCursorPosition();
            }
            else {
                return (Console.CursorLeft, Console.CursorTop);
            }
        }
        #endregion
    }
}

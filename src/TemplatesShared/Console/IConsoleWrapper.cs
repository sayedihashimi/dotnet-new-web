using System;

namespace TemplatesShared {
    public interface IConsoleWrapper {

        string IndentPrefix { get; set; }
        IConsoleWrapper ParentWrapper { get; }
        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }
        bool IsOutputRedirected { get; }
        int BufferWidth { get; set; }
        int BufferHeight { get; set; }

        void Clear();
        void DecreaseIndent();
        (int Left, int Top) GetCursorPosition();
        string GetIndentString();
        void IncreaseIndent();
        int Read();
        ConsoleKeyInfo ReadKey();
        ConsoleKeyInfo ReadKey(bool intercept);
        string ReadLine();
        void SetBufferSize(int width, int height);
        void SetCursorPosition(int left, int top);
        void SetCursorPosition((int cursorLeft, int cursorTop) cursorPosition);
        void Write(char value);
        void Write(object vlaue);
        void Write(string value);
        void WriteIndent();
        void WriteLine();
        void WriteLine(char value);
        void WriteLine(object vlaue);
        void WriteLine(string value);
    }
}
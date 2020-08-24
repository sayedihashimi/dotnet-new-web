using System;

namespace TemplatesShared {
    public interface IConsoleWrapper {
        IConsoleWrapper ParentWrapper { get; }
        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }
        bool IsOutputRedirected { get; }

        void Clear();
        (int Left, int Top) GetCursorPosition();
        int Read();
        ConsoleKeyInfo ReadKey();
        string ReadLine();
        void SetCursorPosition(int left, int top);
        void Write(char value);
        void Write(object vlaue);
        void Write(string value);
        void WriteLine();
        void WriteLine(char value);
        void WriteLine(object vlaue);
        void WriteLine(string value);
    }
}
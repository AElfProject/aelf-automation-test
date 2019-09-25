using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using console = System.Console;

namespace AElfChain.Console.InputOption
{
    public class ConsoleReader
    {
        private readonly ICompletionEngine _completionEngine;
        private readonly char[] _tokenDelimiters;

        public ConsoleReader() : this(new EmptyCompletionEngine()) { }
        public ConsoleReader(ICompletionEngine completionEngine)
        {
            if (completionEngine == null) throw new ArgumentNullException("completionEngine");
            _completionEngine = completionEngine;
            _tokenDelimiters = completionEngine.GetTokenDelimiters();
        }

        public string ReadLine()
        {
            var startLeft = console.CursorLeft;
            var startTop = console.CursorTop;
            var buffer = new StringBuilder();

            var selection = new Selection(buffer, startLeft, startTop);

            string[] completionCandidates = null;
            int completionIndex = -1;

            while (true)
            {
                var bufferIndex = GetBufferIndexFromCursor(startLeft, startTop);
                var keyInfo = console.ReadKey(true);

                if (completionCandidates != null)
                {
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.Escape:
                        case ConsoleKey.Backspace:
                        case ConsoleKey.Delete:
                            completionCandidates = null;
                            completionIndex = -1;
                            Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            break;
                        case ConsoleKey.Tab:
                            if (completionCandidates.Length < 2) break;
                            completionIndex++;
                            if (completionIndex == completionCandidates.Length) completionIndex = 0;
                            bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                            break;
                        case ConsoleKey.Spacebar:
                            completionCandidates = null;
                            completionIndex = -1;
                            selection.Reset(bufferIndex);
                            bufferIndex = Insert(" ", startLeft, startTop, buffer, selection, bufferIndex);
                            break;
                        case ConsoleKey.Enter:
                            console.WriteLine();
                            return buffer.ToString();
                        default:
                            bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                            bufferIndex = Insert(keyInfo.KeyChar.ToString(), startLeft, startTop, buffer, selection, bufferIndex);
                            completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                            break;
                    }
                }
                else
                {
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        console.WriteLine();
                        return buffer.ToString();
                    }
                    if (keyInfo.Key == _completionEngine.Trigger.Key
                        && keyInfo.Modifiers == _completionEngine.Trigger.Modifiers)
                    {
                        completionIndex = 0;
                        completionCandidates = DisplayCompletion(startLeft, startTop, buffer, selection, completionIndex, ref bufferIndex);
                    }
                    else
                    {
                        switch (keyInfo.Key)
                        {
                            case ConsoleKey.LeftArrow:
                                if (bufferIndex == 0)
                                {
                                    if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0)
                                        selection.Reset(bufferIndex);
                                    break;
                                }
                                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                {
                                    bufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                    selection.Reset(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
                                {
                                    bufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                    selection.Resize(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                {
                                    bufferIndex--;
                                    selection.Resize(bufferIndex);
                                }
                                else
                                {
                                    bufferIndex--;
                                    selection.Reset(bufferIndex);
                                }
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                break;
                            case ConsoleKey.RightArrow:
                                if (bufferIndex == buffer.Length)
                                {
                                    if ((keyInfo.Modifiers & ConsoleModifiers.Shift) == 0)
                                        selection.Reset(bufferIndex);
                                    break;
                                }
                                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                {
                                    bufferIndex = GetNextWordBufferIndex(buffer, bufferIndex);
                                    selection.Reset(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
                                {
                                    bufferIndex = GetNextWordBufferIndex(buffer, bufferIndex);
                                    selection.Resize(bufferIndex);
                                }
                                else if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                {
                                    bufferIndex++;
                                    selection.Resize(bufferIndex);
                                }
                                else
                                {
                                    bufferIndex++;
                                    selection.Reset(bufferIndex);
                                }
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                break;
                            case ConsoleKey.Backspace:
                                if (bufferIndex == 0) break;
                                if (selection.Length > 0)
                                {
                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(new string(' ', buffer.Length));

                                    buffer.Remove(selection.Start, selection.Length);
                                    bufferIndex = selection.Start;

                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(buffer.ToString());
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                else
                                {
                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(new string(' ', buffer.Length));
                                    if (keyInfo.Modifiers == ConsoleModifiers.Control)
                                    {
                                        var newBufferIndex = GetPreviousWordBufferIndex(buffer, bufferIndex);
                                        while (bufferIndex > newBufferIndex)
                                        {
                                            bufferIndex--;
                                            buffer.Remove(bufferIndex, 1);
                                        }
                                    }
                                    else
                                    {
                                        bufferIndex--;
                                        buffer.Remove(bufferIndex, 1);
                                    }
                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(buffer.ToString());
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                    selection.Reset(bufferIndex);
                                }
                                break;
                            case ConsoleKey.Delete:
                                if (bufferIndex == buffer.Length) break;
                                bufferIndex = Delete(startLeft, startTop, buffer, selection, bufferIndex, keyInfo);
                                break;
                            case ConsoleKey.Home:
                                bufferIndex = 0;
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                    selection.Resize(bufferIndex);
                                else selection.Reset(bufferIndex);
                                break;
                            case ConsoleKey.End:
                                bufferIndex = buffer.Length;
                                SetCursorPosition(bufferIndex, startLeft, startTop);
                                if (keyInfo.Modifiers == ConsoleModifiers.Shift)
                                    selection.Resize(bufferIndex);
                                else selection.Reset(bufferIndex);
                                break;
                            case ConsoleKey.Enter:
                                console.WriteLine();
                                return buffer.ToString();
                            default:
                                if (selection.Length > 0)
                                {
                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(new string(' ', buffer.Length));

                                    buffer.Remove(selection.Start, selection.Length);
                                    bufferIndex = selection.Start;
                                    buffer.Insert(bufferIndex, keyInfo.KeyChar);

                                    console.SetCursorPosition(startLeft, startTop);
                                    console.Write(buffer.ToString());
                                    bufferIndex++;
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                else
                                {
                                    buffer.Insert(bufferIndex, keyInfo.KeyChar);
                                    bufferIndex++;
                                    console.Write(keyInfo.KeyChar);
                                    console.Write(buffer.ToString(bufferIndex, buffer.Length - bufferIndex));
                                    selection.Reset(bufferIndex);
                                    SetCursorPosition(bufferIndex, startLeft, startTop);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private string[] DisplayCompletion(int startLeft, int startTop, StringBuilder buffer, Selection selection, int completionIndex, ref int bufferIndex)
        {
            string[] completionCandidates;
            var stack = new Stack<char>();
            var index = bufferIndex;
            while (index > 0 && _tokenDelimiters.All(t => t != buffer[index - 1]))
            {
                stack.Push(buffer[index - 1]);
                index--;
            }
            var partial = new string(stack.ToArray());
            completionCandidates = _completionEngine.GetCompletions(partial);
            if (completionCandidates == null || completionCandidates.Length == 0)
            {
                completionCandidates = null;
            }
            else
            {
                var completion = completionCandidates[completionIndex];
                var count = buffer.Length;
                buffer.Clear();
                buffer.Insert(0, completion);
                selection.Reset(bufferIndex - count);

                bufferIndex += completion.Length;
                selection.Resize(bufferIndex - count);
                SetCursorPosition(bufferIndex - count, startLeft, startTop);
            }

            return completionCandidates;
        }

        private static int Insert(string value, int startLeft, int startTop, StringBuilder buffer, Selection selection, int bufferIndex)
        {
            if (selection.Length > 0)
            {
                console.SetCursorPosition(startLeft, startTop);
                console.Write(new string(' ', buffer.Length));

                buffer.Remove(selection.Start, selection.Length);
                bufferIndex = selection.Start;

                buffer.Insert(bufferIndex, value);

                console.SetCursorPosition(startLeft, startTop);
                console.Write(buffer.ToString());

                bufferIndex += value.Length;
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }
            else
            {
                buffer.Insert(bufferIndex, value);

                bufferIndex += value.Length;
                console.Write(value);
                console.Write(buffer.ToString(bufferIndex, buffer.Length - bufferIndex));
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }

            return bufferIndex;
        }

        private static int Delete(int startLeft, int startTop, StringBuilder buffer, Selection selection, int bufferIndex, ConsoleKeyInfo keyInfo)
        {
            if (selection.Length > 0)
            {
                console.SetCursorPosition(startLeft, startTop);
                console.Write(new string(' ', buffer.Length));

                buffer.Remove(selection.Start, selection.Length);
                bufferIndex = selection.Start;

                console.SetCursorPosition(startLeft, startTop);
                console.Write(buffer.ToString());
                selection.Reset(bufferIndex);
                SetCursorPosition(bufferIndex, startLeft, startTop);
            }
            else
            {
                console.SetCursorPosition(startLeft, startTop);
                console.Write(new string(' ', buffer.Length));
                if (keyInfo.Modifiers == ConsoleModifiers.Control)
                {
                    var sizeToDelete = GetNextWordBufferIndex(buffer, bufferIndex) - bufferIndex;
                    for (int i = 0; i < sizeToDelete; i++)
                    {
                        buffer.Remove(bufferIndex, 1);
                    }
                }
                else buffer.Remove(bufferIndex, 1);
                console.SetCursorPosition(startLeft, startTop);
                console.Write(buffer.ToString());
                SetCursorPosition(bufferIndex, startLeft, startTop);
                selection.Reset(bufferIndex);
            }

            return bufferIndex;
        }

        private static int GetPreviousWordBufferIndex(StringBuilder buffer, int bufferIndex)
        {
            // If spaces are to the left, go past them first.
            while (bufferIndex > 0 && buffer[bufferIndex - 1] == ' ')
            {
                bufferIndex = Math.Max(bufferIndex - 2, 0);
            }

            // Go left past any non-spaces until the char to the left is a space.
            for (bufferIndex--; bufferIndex >= 0; bufferIndex--)
            {
                if (buffer[bufferIndex] == ' ')
                {
                    bufferIndex++;
                    break;
                }
            }

            return Math.Max(bufferIndex, 0);
        }

        private static int GetNextWordBufferIndex(StringBuilder buffer, int bufferIndex)
        {
            // If there are any non-spaces to the right, go past them first.
            while (bufferIndex < buffer.Length - 1 && buffer[bufferIndex + 1] != ' ')
            {
                bufferIndex = Math.Min(bufferIndex + 1, buffer.Length);
            }

            // Go right past any spaces until the char to the right is a non-space.
            for (bufferIndex++; bufferIndex < buffer.Length; bufferIndex++)
            {
                if (buffer[bufferIndex] != ' ')
                {
                    break;
                }
            }

            return bufferIndex;
        }

        private int GetBufferIndexFromCursor(int startLeft, int startTop)
        {
            var left = startLeft;
            var top = startTop;
            int bufferIndex;
            var sanityCheck = console.WindowWidth * console.WindowHeight;
            for (bufferIndex = 0;
                 (left != console.CursorLeft || top != console.CursorTop)
                    && bufferIndex <= sanityCheck;
                bufferIndex++)
            {
                left++;
                if (left >= console.WindowWidth)
                {
                    left = 0;
                    top++;
                }
            }
            return bufferIndex;
        }

        private static void SetCursorPosition(int bufferIndex, int startLeft, int startTop)
        {
            int left, top;
            GetCursorPosition(bufferIndex, startLeft, startTop, out left, out top);
            console.SetCursorPosition(left, top);
        }

        private static void GetCursorPosition(int bufferIndex, int startLeft, int startTop, out int left, out int top)
        {
            left = startLeft;
            top = startTop;

            for (int i = 0; i < bufferIndex; i++)
            {
                left++;
                if (left >= console.WindowWidth)
                {
                    left = 0;
                    top++;
                }
            }
        }

        private class Selection
        {
            private readonly StringBuilder _buffer;
            private readonly int _startLeft;
            private readonly int _startTop;

            public Selection(StringBuilder buffer, int startLeft, int startTop)
            {
                _buffer = buffer;
                _startLeft = startLeft;
                _startTop = startTop;
            }

            public void Reset(int bufferIndex)
            {
                if (Beginning != End)
                {
                    console.SetCursorPosition(_startLeft, _startTop);
                    console.Write(_buffer.ToString());
                    SetCursorPosition(End, _startLeft, _startTop);
                }
                Beginning = bufferIndex;
                End = Beginning;
            }

            public void Resize(int bufferIndex)
            {
                // Clear any old formatting.
                console.SetCursorPosition(_startLeft, _startTop);
                console.Write(_buffer.ToString());

                End = bufferIndex;
                var index = IsExpandingToRight(bufferIndex) ? Beginning : bufferIndex;
                SetCursorPosition(index, _startLeft, _startTop);

                // Swap the console's foreground and background colors.
                ConsoleColor originalForegroundColor = console.ForegroundColor;
                console.ForegroundColor = console.BackgroundColor;
                console.BackgroundColor = originalForegroundColor;

                // Write this selection's portion of the buffer using the swapped colors.
                console.Write(_buffer.ToString(Start, Length));

                // Swap the colors back to their original colors.
                console.BackgroundColor = console.ForegroundColor;
                console.ForegroundColor = originalForegroundColor;

                SetCursorPosition(bufferIndex, _startLeft, _startTop);
            }

            public int Beginning { get; set; }
            public int End { get; set; }
            public int Start => Beginning < End ? Beginning : End;
            public int Length => Math.Abs(End - Beginning);
            public bool IsExpandingToRight(int bufferIndex) => (Beginning == End && bufferIndex > End) || Beginning < End;
        }
    }
}
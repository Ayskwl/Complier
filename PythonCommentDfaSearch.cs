using System;
using System.Collections.Generic;

namespace RGRCompilator
{
    public class DfaSearchResult
    {
        public string FoundText { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public string Location
        {
            get { return $"строка {Line}, позиция {Column}"; }
        }
    }

    public enum PythonCommentState
    {
        Start,
        SingleLineComment,
        TripleDoubleComment,
        TripleSingleComment
    }

    public static class PythonCommentDfaSearch
    {
        public static List<DfaSearchResult> Search(string text)
        {
            var results = new List<DfaSearchResult>();

            if (string.IsNullOrEmpty(text))
                return results;

            PythonCommentState state = PythonCommentState.Start;

            int startIndex = -1;
            int i = 0;

            while (i < text.Length)
            {
                switch (state)
                {
                    case PythonCommentState.Start:
                        {
                            if (text[i] == '#')
                            {
                                startIndex = i;
                                state = PythonCommentState.SingleLineComment;
                                i++;
                            }
                            else if (IsTripleDoubleQuote(text, i))
                            {
                                startIndex = i;
                                state = PythonCommentState.TripleDoubleComment;
                                i += 3;
                            }
                            else if (IsTripleSingleQuote(text, i))
                            {
                                startIndex = i;
                                state = PythonCommentState.TripleSingleComment;
                                i += 3;
                            }
                            else
                            {
                                i++;
                            }

                            break;
                        }

                    case PythonCommentState.SingleLineComment:
                        {
                            if (text[i] == '\n')
                            {
                                results.Add(Build(text, startIndex, i - startIndex));
                                state = PythonCommentState.Start;
                            }

                            i++;
                            break;
                        }

                    case PythonCommentState.TripleDoubleComment:
                        {
                            if (IsTripleDoubleQuote(text, i))
                            {
                                i += 3;
                                results.Add(Build(text, startIndex, i - startIndex));
                                state = PythonCommentState.Start;
                            }
                            else
                            {
                                i++;
                            }

                            break;
                        }

                    case PythonCommentState.TripleSingleComment:
                        {
                            if (IsTripleSingleQuote(text, i))
                            {
                                i += 3;
                                results.Add(Build(text, startIndex, i - startIndex));
                                state = PythonCommentState.Start;
                            }
                            else
                            {
                                i++;
                            }

                            break;
                        }
                }
            }

            // Если однострочный комментарий дошёл до конца файла без \n
            if (state == PythonCommentState.SingleLineComment && startIndex >= 0)
            {
                results.Add(Build(text, startIndex, text.Length - startIndex));
            }

            // Незакрытые многострочные комментарии не добавляем,
            // потому что комментарий считается корректным только при наличии закрывающих кавычек.
            // Если захочешь, потом можно сделать отдельный режим,
            // где они будут показываться как ошибка.

            return results;
        }

        private static bool IsTripleDoubleQuote(string text, int index)
        {
            return index + 2 < text.Length &&
                   text[index] == '"' &&
                   text[index + 1] == '"' &&
                   text[index + 2] == '"';
        }

        private static bool IsTripleSingleQuote(string text, int index)
        {
            return index + 2 < text.Length &&
                   text[index] == '\'' &&
                   text[index + 1] == '\'' &&
                   text[index + 2] == '\'';
        }

        private static DfaSearchResult Build(string text, int start, int length)
        {
            var pos = GetPosition(text, start);

            return new DfaSearchResult
            {
                FoundText = text.Substring(start, length),
                StartIndex = start,
                Length = length,
                Line = pos.line,
                Column = pos.column
            };
        }

        private static (int line, int column) GetPosition(string text, int index)
        {
            int line = 1;
            int column = 1;

            for (int i = 0; i < index; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }
    }
}
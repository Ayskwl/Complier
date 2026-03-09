using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RGRCompilator
{
    public enum ScannerState
    {
        Start,
        Identifier,
        Number,
        WhiteSpace,
        Error
    }

    public class Lexeme
    {
        public int Code { get; set; }
        public string TypeName { get; set; }
        public string Value { get; set; }

        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }

        public int StartIndex { get; set; }
        public int Length { get; set; }

        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public string Location
        {
            get { return $"строка {Line}, {StartColumn}-{EndColumn}"; }
        }
    }

    public class LexicalAnalyzer
    {
        private readonly HashSet<string> _keywords = new HashSet<string>
        {
            "struct",
            "typedef",
            "int",
            "char",
            "float",
            "double",
            "short",
            "long",
            "signed",
            "unsigned",
            "void",
            "const"
        };

        private readonly Dictionary<char, (int code, string typeName)> _singleCharTokens =
            new Dictionary<char, (int code, string typeName)>
            {
                { '{', (11, "разделитель") },
                { '}', (11, "разделитель") },
                { '[', (11, "разделитель") },
                { ']', (11, "разделитель") },
                { '(', (11, "разделитель") },
                { ')', (11, "разделитель") },
                { ';', (16, "конец оператора") },
                { ',', (11, "разделитель") },

                { '=', (10, "оператор присваивания") },
                { '*', (10, "оператор") },
                { '&', (10, "оператор") },
                { '.', (10, "оператор") }
            };

        public List<Lexeme> Analyze(string text)
        {
            var result = new List<Lexeme>();

            if (text == null)
                text = string.Empty;

            int i = 0;
            int line = 1;
            int col = 1;

            while (i < text.Length)
            {
                char ch = text[i];

                if (char.IsWhiteSpace(ch))
                {
                    int startIndex = i;
                    int startLine = line;
                    int startCol = col;

                    StringBuilder sb = new StringBuilder();

                    while (i < text.Length && char.IsWhiteSpace(text[i]))
                    {
                        sb.Append(text[i]);

                        if (text[i] == '\r')
                        {
                            i++;

                            if (i < text.Length && text[i] == '\n')
                            {
                                sb.Append(text[i]);
                                i++;
                            }

                            line++;
                            col = 1;
                        }
                        else if (text[i] == '\n')
                        {
                            i++;
                            line++;
                            col = 1;
                        }
                        else
                        {
                            i++;
                            col++;
                        }
                    }

                    int endLine = startLine;
                    int endCol;

                    string wsText = sb.ToString();
                    string displayValue = GetWhiteSpaceDisplay(wsText);

                    if (wsText.Contains("\r") || wsText.Contains("\n"))
                    {
                        endCol = 1;
                    }
                    else
                    {
                        endCol = startCol + wsText.Length - 1;
                    }

                    result.Add(new Lexeme
                    {
                        Code = 11,
                        TypeName = "разделитель",
                        Value = displayValue,
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = endCol,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    int startIndex = i;
                    int startLine = line;
                    int startCol = col;

                    StringBuilder sb = new StringBuilder();

                    while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                    {
                        sb.Append(text[i]);
                        i++;
                        col++;
                    }

                    string lexemeText = sb.ToString();
                    bool isKeyword = _keywords.Contains(lexemeText);

                    result.Add(new Lexeme
                    {
                        Code = isKeyword ? 14 : 2,
                        TypeName = isKeyword ? "ключевое слово" : "идентификатор",
                        Value = lexemeText,
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = col - 1,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                if (char.IsDigit(ch))
                {
                    int startIndex = i;
                    int startLine = line;
                    int startCol = col;

                    StringBuilder sb = new StringBuilder();

                    while (i < text.Length && char.IsDigit(text[i]))
                    {
                        sb.Append(text[i]);
                        i++;
                        col++;
                    }

                    result.Add(new Lexeme
                    {
                        Code = 1,
                        TypeName = "целое без знака",
                        Value = sb.ToString(),
                        Line = startLine,
                        StartColumn = startCol,
                        EndColumn = col - 1,
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        IsError = false
                    });

                    continue;
                }

                if (_singleCharTokens.ContainsKey(ch))
                {
                    var info = _singleCharTokens[ch];

                    result.Add(new Lexeme
                    {
                        Code = info.code,
                        TypeName = info.typeName,
                        Value = ch.ToString(),
                        Line = line,
                        StartColumn = col,
                        EndColumn = col,
                        StartIndex = i,
                        Length = 1,
                        IsError = false
                    });

                    i++;
                    col++;
                    continue;
                }

                result.Add(new Lexeme
                {
                    Code = 99,
                    TypeName = "ошибка",
                    Value = ch.ToString(),
                    Line = line,
                    StartColumn = col,
                    EndColumn = col,
                    StartIndex = i,
                    Length = 1,
                    IsError = true,
                    ErrorMessage = $"Недопустимый символ: '{ch}'"
                });

                i++;
                col++;
            }

            return result;
        }

        private string GetWhiteSpaceDisplay(string value)
        {
            if (value == " ")
                return "(пробел)";
            if (value == "\t")
                return "(табуляция)";
            if (value == "\r" || value == "\n" || value == "\r\n")
                return "(перевод строки)";

            if (value.All(c => c == ' '))
                return "(пробелы)";
            if (value.Contains("\t") && !value.Contains("\r") && !value.Contains("\n"))
                return "(табуляция)";
            if (value.Contains("\r") || value.Contains("\n"))
                return "(перевод строки)";

            return "(разделитель)";
        }
    }
}
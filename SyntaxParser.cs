using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RGRCompilator
{
    public class ParserError
    {
        public string InvalidFragment { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public string Description { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }

        public string Location
        {
            get { return $"строка {Line}, позиция {Position}"; }
        }
    }

    public class ParserResult
    {
        public bool Success { get; set; }
        public List<ParserError> Errors { get; set; } = new List<ParserError>();
    }

    public class SyntaxParser
    {
        private readonly List<Lexeme> _tokens;
        private readonly List<ParserError> _errors = new List<ParserError>();
        private int _position;

        private static readonly string[] ValidTypes =
        {
            "int", "char", "float", "double", "short", "long"
        };

        public SyntaxParser(List<Lexeme> sourceTokens)
        {
            _tokens = PrepareTokens(sourceTokens);
            _position = 0;
        }

        public ParserResult Parse()
        {
            if (_tokens.Count == 0)
            {
                AddEndError("Введён пустой текст");
                return BuildResult();
            }

            ParseStruct();

            if (!IsAtEnd())
            {
                AddTrailingFragmentError();
            }

            return BuildResult();
        }

        private ParserResult BuildResult()
        {
            return new ParserResult
            {
                Success = _errors.Count == 0,
                Errors = _errors
            };
        }
        private void ParseStruct()
        {
            bool hasStructKeyword = false;

            if (MatchValue("struct"))
            {
                hasStructKeyword = true;
            }
            else
            {
                AddError(Current, "Ожидалось ключевое слово struct");
            }

            if (Current != null && IsIdentifier(Current))
            {
                Advance();
            }
            else
            {
                if (hasStructKeyword)
                {
                    AddError(Current, "Ожидалось имя структуры");
                    RecoverTo("{", "}", ";", "int", "char", "float", "double", "short", "long");
                }
            }
            
            if (MatchValue("{"))
            {

            }
            else
            {
                AddError(Current, "Ожидалась открывающая фигурная скобка '{'");

                if (!IsFieldStart(Current))
                {
                    RecoverTo("{", "}", ";", "int", "char", "float", "double", "short", "long");

                    if (Current != null && Current.Value == "{")
                    {
                        Advance();
                    }
                }
            }

            ParseFields();

            if (IsAtEnd())
            {
                AddEndError("Ожидалась закрывающая фигурная скобка '}'");
                return;
            }

            if (!MatchValue("}"))
            {
                AddError(Current, "Ожидалась закрывающая фигурная скобка '}'");
                RecoverTo("}", ";");

                if (Current != null && Current.Value == "}")
                {
                    Advance();
                }
            }

            if (IsAtEnd())
            {
                AddEndError("Ожидалась точка с запятой ';' после определения структуры");
                return;
            }

            if (!MatchValue(";"))
            {
                AddError(Current, "Ожидалась точка с запятой ';' после определения структуры");
                RecoverTo(";");

                if (Current != null && Current.Value == ";")
                {
                    Advance();
                }
            }
        }

        private void ParseFields()
        {
            while (!IsAtEnd() && Current.Value != "}")
            {
                ParseField();
            }
        }

        private void ParseField()
        {
            if (Current == null)
                return;

            if (!IsType(Current))
            {
                AddError(Current, "Ожидалось описание поля: тип данных");
                RecoverField();
                return;
            }

            Advance();

            if (!MatchIdentifier())
            {
                AddError(Current, "Ожидалось имя поля");

                if (Current != null && (Current.Value == ";" || Current.Value == "}"))
                {
                    if (Current.Value == ";")
                        Advance();
                    return;
                }

                RecoverField();
                return;
            }

            if (!MatchValue(";"))
            {
                AddError(Current, "Ожидалась точка с запятой ';' после описания поля");

                if (Current != null && (IsType(Current) || Current.Value == "}"))
                    return;

                RecoverField();
            }
        }

        private void RecoverField()
        {
            while (!IsAtEnd())
            {
                if (Current.Value == "}")
                    return;

                if (Current.Value == ";")
                {
                    Advance();
                    return;
                }

                if (IsType(Current))
                    return;

                Advance();
            }
        }

        private void RecoverTo(params string[] syncTokens)
        {
            while (!IsAtEnd() && !syncTokens.Contains(Current.Value))
            {
                Advance();
            }
        }

        private bool MatchValue(string value)
        {
            if (Current != null && Current.Value == value)
            {
                Advance();
                return true;
            }

            return false;
        }

        private bool MatchIdentifier()
        {
            if (Current != null && IsIdentifier(Current))
            {
                Advance();
                return true;
            }

            return false;
        }

        private bool IsIdentifier(Lexeme token)
        {
            return token != null && token.TypeName == "идентификатор";
        }

        private bool IsType(Lexeme token)
        {
            return token != null && ValidTypes.Contains(token.Value);
        }

        private bool IsFieldStart(Lexeme token)
        {
            return IsType(token);
        }

        private void AddTrailingFragmentError()
        {
            if (IsAtEnd())
                return;

            Lexeme first = Current;
            StringBuilder sb = new StringBuilder();
            int startIndex = first.StartIndex;
            int totalLength = 0;

            while (!IsAtEnd())
            {
                sb.Append(Current.Value);
                totalLength += Math.Max(1, Current.Length);
                Advance();
            }

            AddErrorInternal(
                sb.ToString(),
                first.Line,
                first.StartColumn,
                "Лишний фрагмент после завершения определения структуры",
                startIndex,
                totalLength
            );
        }

        private void AddEndError(string description)
        {
            if (_tokens.Count == 0)
            {
                AddErrorInternal("(конец строки)", 1, 1, description, 0, 1);
                return;
            }

            Lexeme last = _tokens[_tokens.Count - 1];
            int startIndex = last.StartIndex + Math.Max(1, last.Length);

            AddErrorInternal(
                "(конец строки)",
                last.Line,
                last.EndColumn + 1,
                description,
                startIndex,
                1
            );
        }

        private void AddError(Lexeme token, string description)
        {
            if (token == null)
            {
                AddEndError(description);
                return;
            }

            AddErrorInternal(
                token.Value,
                token.Line,
                token.StartColumn,
                description,
                token.StartIndex,
                token.Length > 0 ? token.Length : 1
            );
        }

        private void AddErrorInternal(string invalidFragment, int line, int position, string description, int startIndex, int length)
        {
            ParserError last = _errors.Count > 0 ? _errors[_errors.Count - 1] : null;

            if (last != null &&
                last.StartIndex == startIndex &&
                last.Description == description)
            {
                return;
            }

            _errors.Add(new ParserError
            {
                InvalidFragment = invalidFragment,
                Line = line,
                Position = position,
                Description = description,
                StartIndex = startIndex,
                Length = Math.Max(1, length)
            });
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
        }

        private void Advance()
        {
            if (!IsAtEnd())
                _position++;
        }

        private Lexeme Current
        {
            get
            {
                if (_position < _tokens.Count)
                    return _tokens[_position];
                return null;
            }
        }

        private List<Lexeme> PrepareTokens(List<Lexeme> source)
        {
            var result = new List<Lexeme>();

            if (source == null)
                return result;

            foreach (var lex in source)
            {
                if (lex == null)
                    continue;

                if (lex.IsError)
                    continue;

                if (IsIgnorable(lex))
                    continue;

                result.Add(lex);
            }

            return result;
        }

        private bool IsIgnorable(Lexeme lex)
        {
            if (lex == null)
                return true;

            return lex.Value == "(пробел)"
                || lex.Value == "(пробелы)"
                || lex.Value == "(табуляция)"
                || lex.Value == "(перевод строки)";
        }
    }
}
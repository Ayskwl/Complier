using System;
using System.Collections.Generic;
using System.Linq;

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

        public SyntaxParser(List<Lexeme> sourceTokens)
        {
            _tokens = PrepareTokens(sourceTokens);
            _position = 0;
        }

        public ParserResult Parse()
        {
            if (_tokens.Count == 0)
            {
                AddError(null, "Пустой ввод");
                return new ParserResult
                {
                    Success = false,
                    Errors = _errors
                };
            }

            ParseStructDef();

            while (!IsAtEnd())
            {
                AddError(Current, "Лишний фрагмент после завершения определения структуры");
                Advance();
            }

            return new ParserResult
            {
                Success = _errors.Count == 0,
                Errors = _errors
            };
        }

        private void ParseStructDef()
        {
            if (!MatchValue("struct"))
            {
                AddError(Current, "Ожидалось ключевое слово struct");
                RecoverTo("struct", "{", "}", ";");

                if (Current != null && Current.Value == "struct")
                    Advance();
            }

            if (!MatchIdentifier())
            {
                AddError(Current, "Ожидалось имя структуры");
                RecoverTo("{", "}", ";");
            }

            if (!MatchValue("{"))
            {
                AddError(Current, "Ожидалась открывающая фигурная скобка '{'");
                RecoverTo("{", "}", ";", "int", "char", "float", "double", "short", "long", "signed", "unsigned", "void");

                if (Current != null && Current.Value == "{")
                    Advance();
            }

            ParseFieldList();

            if (!MatchValue("}"))
            {
                AddError(Current, "Ожидалась закрывающая фигурная скобка '}'");
                RecoverTo("}", ";");

                if (Current != null && Current.Value == "}")
                    Advance();
            }

            if (!MatchValue(";"))
            {
                AddError(Current, "Ожидалась точка с запятой ';' после определения структуры");
                RecoverTo(";");
                if (Current != null && Current.Value == ";")
                    Advance();
            }
        }

        private void ParseFieldList()
        {
            while (!IsAtEnd() && Current.Value != "}")
            {
                ParseField();
            }
        }

        private void ParseField()
        {
            if (!IsType(Current))
            {
                AddError(Current, "Ожидался тип поля");
                RecoverField();
                return;
            }

            Advance();

            if (!MatchIdentifier())
            {
                AddError(Current, "Ожидалось имя поля");
                RecoverField();
                return;
            }

            if (!MatchValue(";"))
            {
                AddError(Current, "Ожидалась точка с запятой ';' после описания поля");
                RecoverField();
            }
        }

        private void RecoverField()
        {
            while (!IsAtEnd() && Current.Value != ";" && Current.Value != "}")
            {
                Advance();
            }

            if (!IsAtEnd() && Current.Value == ";")
                Advance();
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
            if (Current != null && Current.TypeName == "идентификатор")
            {
                Advance();
                return true;
            }

            return false;
        }

        private bool IsType(Lexeme token)
        {
            if (token == null)
                return false;

            string[] validTypes =
            {
                "int", "char", "float", "double", "short", "long", "signed", "unsigned", "void"
            };

            return validTypes.Contains(token.Value);
        }

        private void AddError(Lexeme token, string description)
        {
            if (token != null)
            {
                _errors.Add(new ParserError
                {
                    InvalidFragment = token.Value,
                    Line = token.Line,
                    Position = token.StartColumn,
                    Description = description,
                    StartIndex = token.StartIndex,
                    Length = token.Length > 0 ? token.Length : 1
                });
            }
            else
            {
                _errors.Add(new ParserError
                {
                    InvalidFragment = "(конец строки)",
                    Line = 1,
                    Position = 1,
                    Description = description,
                    StartIndex = 0,
                    Length = 1
                });
            }
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
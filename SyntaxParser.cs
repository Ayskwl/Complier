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

    public enum ParserState
    {
        ExpectStruct,
        ExpectStructName,
        ExpectOpenBrace,
        ExpectFieldTypeOrCloseBrace,
        ExpectFieldName,
        ExpectFieldSemicolon,
        ExpectFinalSemicolon,
        Done
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
                _errors.Add(new ParserError
                {
                    InvalidFragment = "",
                    Line = 1,
                    Position = 1,
                    Description = "Введён пустой текст",
                    StartIndex = 0,
                    Length = 0
                });

                return new ParserResult
                {
                    Success = false,
                    Errors = _errors
                };
            }

            ParserState state = ParserState.ExpectStruct;

            while (state != ParserState.Done)
            {
                switch (state)
                {
                    case ParserState.ExpectStruct:
                        if (MatchValue("struct"))
                        {
                            state = ParserState.ExpectStructName;
                        }
                        else
                        {
                            AddError(Current, "Ожидалось ключевое слово struct");

                            if (!RecoverTo("struct", "{", "}", ";"))
                            {
                                state = ParserState.Done;
                            }
                            else if (Current != null && Current.Value == "struct")
                            {
                                Advance();
                                state = ParserState.ExpectStructName;
                            }
                            else
                            {
                                SafeAdvanceOrFinish(ref state);
                            }
                        }
                        break;

                    case ParserState.ExpectStructName:
                        if (MatchIdentifier())
                        {
                            state = ParserState.ExpectOpenBrace;
                        }
                        else
                        {
                            AddError(Current, "Ожидалось имя структуры");

                            if (!RecoverTo("{", "}", ";"))
                            {
                                state = ParserState.Done;
                            }
                            else
                            {
                                state = ParserState.ExpectOpenBrace;
                            }
                        }
                        break;

                    case ParserState.ExpectOpenBrace:
                        if (MatchValue("{"))
                        {
                            state = ParserState.ExpectFieldTypeOrCloseBrace;
                        }
                        else
                        {
                            AddError(Current, "Ожидалась открывающая фигурная скобка '{'");

                            if (!RecoverTo("{", "}", ";", "int", "char", "float", "double", "short", "long"))
                            {
                                state = ParserState.Done;
                            }
                            else if (Current != null && Current.Value == "{")
                            {
                                Advance();
                                state = ParserState.ExpectFieldTypeOrCloseBrace;
                            }
                            else
                            {
                                state = ParserState.ExpectFieldTypeOrCloseBrace;
                            }
                        }
                        break;

                    case ParserState.ExpectFieldTypeOrCloseBrace:
                        if (IsAtEnd())
                        {
                            AddError(null, "Ожидалась закрывающая фигурная скобка '}'");
                            state = ParserState.Done;
                        }
                        else if (Current.Value == "}")
                        {
                            Advance();
                            state = ParserState.ExpectFinalSemicolon;
                        }
                        else if (IsType(Current))
                        {
                            Advance();
                            state = ParserState.ExpectFieldName;
                        }
                        else
                        {
                            AddError(Current, "Ожидался тип поля или закрывающая фигурная скобка '}'");
                            RecoverField();

                            if (IsAtEnd())
                            {
                                state = ParserState.Done;
                            }
                            else if (Current.Value == "}")
                            {
                                Advance();
                                state = ParserState.ExpectFinalSemicolon;
                            }
                            else
                            {
                                state = ParserState.ExpectFieldTypeOrCloseBrace;
                            }
                        }
                        break;

                    case ParserState.ExpectFieldName:
                        if (MatchIdentifier())
                        {
                            state = ParserState.ExpectFieldSemicolon;
                        }
                        else
                        {
                            AddError(Current, "Ожидалось имя поля");
                            RecoverField();

                            if (IsAtEnd())
                            {
                                state = ParserState.Done;
                            }
                            else if (Current.Value == "}")
                            {
                                Advance();
                                state = ParserState.ExpectFinalSemicolon;
                            }
                            else
                            {
                                state = ParserState.ExpectFieldTypeOrCloseBrace;
                            }
                        }
                        break;

                    case ParserState.ExpectFieldSemicolon:
                        if (MatchValue(";"))
                        {
                            state = ParserState.ExpectFieldTypeOrCloseBrace;
                        }
                        else
                        {
                            AddError(Current, "Ожидалась точка с запятой ';' после описания поля");
                            RecoverField();

                            if (IsAtEnd())
                            {
                                state = ParserState.Done;
                            }
                            else if (Current.Value == "}")
                            {
                                Advance();
                                state = ParserState.ExpectFinalSemicolon;
                            }
                            else
                            {
                                state = ParserState.ExpectFieldTypeOrCloseBrace;
                            }
                        }
                        break;

                    case ParserState.ExpectFinalSemicolon:
                        if (MatchValue(";"))
                        {
                            state = ParserState.Done;
                        }
                        else
                        {
                            AddError(Current, "Ожидалась точка с запятой ';' после определения структуры");
                            state = ParserState.Done;
                        }
                        break;
                }
            }

            if (!IsAtEnd())
            {
                AddTrailingFragmentError();
            }

            if (_errors.Count == 0)
            {
                _errors.Add(new ParserError
                {
                    InvalidFragment = "",
                    Line = 0,
                    Position = 0,
                    Description = "Синтаксических ошибок не обнаружено",
                    StartIndex = 0,
                    Length = 0
                });

                return new ParserResult
                {
                    Success = true,
                    Errors = _errors
                };
            }

            return new ParserResult
            {
                Success = false,
                Errors = _errors
            };
        }

        private void SafeAdvanceOrFinish(ref ParserState state)
        {
            if (!IsAtEnd())
                Advance();

            if (IsAtEnd())
                state = ParserState.Done;
        }

        private void RecoverField()
        {
            while (!IsAtEnd() && Current.Value != ";" && Current.Value != "}")
            {
                Advance();
            }

            if (!IsAtEnd() && Current.Value == ";")
            {
                Advance();
            }
        }

        private bool RecoverTo(params string[] syncTokens)
        {
            while (!IsAtEnd() && !syncTokens.Contains(Current.Value))
            {
                Advance();
            }

            return !IsAtEnd();
        }

        private void AddTrailingFragmentError()
        {
            if (IsAtEnd())
                return;

            Lexeme first = Current;
            StringBuilder fragment = new StringBuilder();
            int startIndex = first.StartIndex;
            int line = first.Line;
            int position = first.StartColumn;
            int totalLength = 0;

            while (!IsAtEnd())
            {
                fragment.Append(Current.Value);
                totalLength += Math.Max(1, Current.Length);
                Advance();
            }

            _errors.Add(new ParserError
            {
                InvalidFragment = fragment.ToString(),
                Line = line,
                Position = position,
                Description = "Лишний фрагмент после завершения определения структуры",
                StartIndex = startIndex,
                Length = totalLength
            });
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
                "int", "char", "float", "double", "short", "long"
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
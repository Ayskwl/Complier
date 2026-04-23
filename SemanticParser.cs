using System.Collections.Generic;

namespace RGRCompilator
{
    public class SemanticParser
    {
        private readonly List<Lexeme> _tokens;
        private readonly SymbolTable _symbolTable = new SymbolTable();
        private int _position;

        private static readonly string[] ValidTypes =
        {
            "int", "char", "float", "double", "short", "long"
        };

        private bool _syntaxErrorAdded = false;

        public SemanticParser(List<Lexeme> sourceTokens)
        {
            _tokens = PrepareTokens(sourceTokens);
            _position = 0;
        }

        public SemanticResult Analyze(List<Lexeme> tokensFromLexer = null)
        {
            SemanticResult result = new SemanticResult();
            _symbolTable.Clear();
            _syntaxErrorAdded = false;

            if (_tokens == null || _tokens.Count == 0)
            {
                result.Errors.Add(new SemanticError
                {
                    Message = "Пустой текст",
                    Line = 1,
                    Position = 1,
                    StartIndex = 0,
                    Length = 1
                });

                result.Success = false;
                result.AstText = "Дерево не построено.";
                return result;
            }

            SemanticAstNode root = ParseStruct(result);

            result.Root = root;
            result.AstText = SemanticAstPrinter.Print(root);
            result.Success = result.Errors.Count == 0;

            return result;
        }

        private SemanticAstNode ParseStruct(SemanticResult result)
        {
            if (!MatchValue("struct"))
            {
                AddSyntaxError(result, Current);
                return null;
            }

            if (Current == null || !IsIdentifier(Current))
            {
                AddSyntaxError(result, Current);
                return null;
            }

            Lexeme structNameLex = Current;
            Advance();

            StructDeclNode structNode = new StructDeclNode(structNameLex.Value);

            if (!MatchValue("{"))
            {
                AddSyntaxError(result, Current);
                return structNode;
            }

            while (!IsAtEnd() && Current != null && Current.Value != "}")
            {
                int oldPosition = _position;

                FieldDeclNode fieldNode = ParseField(result);

                if (fieldNode != null)
                    structNode.Children.Add(fieldNode);

                if (_position == oldPosition)
                    break;
            }

            if (!MatchValue("}"))
            {
                AddSyntaxError(result, Current);
                return structNode;
            }

            if (!MatchValue(";"))
            {
                AddSyntaxError(result, Current);
                return structNode;
            }

            return structNode;
        }

        private FieldDeclNode ParseField(SemanticResult result)
        {
            if (Current == null)
                return null;

            if (!IsType(Current))
            {
                AddSyntaxError(result, Current);
                SkipToFieldEnd();
                return null;
            }

            Lexeme typeLex = Current;
            Advance();

            if (Current == null || !IsIdentifier(Current))
            {
                AddSyntaxError(result, Current);
                SkipToFieldEnd();
                return null;
            }

            Lexeme idLex = Current;
            string fieldName = idLex.Value;
            Advance();

            if (!MatchValue(";"))
            {
                AddSyntaxError(result, Current);
                SkipToFieldEnd();
                return null;
            }

            if (_symbolTable.CheckDuplicate(fieldName))
            {
                SymbolInfo firstDecl = _symbolTable.Lookup(fieldName);

                string extraInfo = firstDecl != null
                    ? $" (строка {firstDecl.Line})"
                    : "";

                AddError(
                    result,
                    idLex,
                    $"Ошибка: идентификатор \"{fieldName}\" уже объявлен ранее{extraInfo}"
                );

                return null;
            }

            _symbolTable.Declare(
                fieldName,
                typeLex.Value,
                null,
                idLex.Line,
                idLex.StartColumn
            );

            return new FieldDeclNode(typeLex.Value, fieldName);
        }

        private void AddSyntaxError(SemanticResult result, Lexeme lex)
        {
            if (_syntaxErrorAdded)
                return;

            _syntaxErrorAdded = true;
            result.HasSyntaxError = true;

            if (lex == null)
            {
                result.Errors.Add(new SemanticError
                {
                    Message = "Допущена синтаксическая ошибка",
                    Line = 1,
                    Position = 1,
                    StartIndex = 0,
                    Length = 1
                });
                return;
            }

            result.Errors.Add(new SemanticError
            {
                Message = "Допущена синтаксическая ошибка",
                Line = lex.Line,
                Position = lex.StartColumn,
                StartIndex = lex.StartIndex,
                Length = lex.Length > 0 ? lex.Length : 1
            });
        }

        private void AddError(SemanticResult result, Lexeme lex, string message)
        {
            if (result == null)
                return;

            if (lex == null)
            {
                result.Errors.Add(new SemanticError
                {
                    Message = message,
                    Line = 1,
                    Position = 1,
                    StartIndex = 0,
                    Length = 1
                });
                return;
            }

            result.Errors.Add(new SemanticError
            {
                Message = message,
                Line = lex.Line,
                Position = lex.StartColumn,
                StartIndex = lex.StartIndex,
                Length = lex.Length > 0 ? lex.Length : 1
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

        private bool IsIdentifier(Lexeme token)
        {
            return token != null && token.TypeName == "идентификатор";
        }

        private bool IsType(Lexeme token)
        {
            if (token == null)
                return false;

            for (int i = 0; i < ValidTypes.Length; i++)
            {
                if (ValidTypes[i] == token.Value)
                    return true;
            }

            return false;
        }

        private void Advance()
        {
            if (!IsAtEnd())
                _position++;
        }

        private bool IsAtEnd()
        {
            return _position >= _tokens.Count;
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

        private void SkipToFieldEnd()
        {
            while (!IsAtEnd() && Current != null && Current.Value != ";" && Current.Value != "}")
            {
                Advance();
            }

            if (Current != null && Current.Value == ";")
                Advance();
        }

        private List<Lexeme> PrepareTokens(List<Lexeme> source)
        {
            List<Lexeme> result = new List<Lexeme>();

            if (source == null)
                return result;

            foreach (Lexeme lex in source)
            {
                if (lex == null)
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
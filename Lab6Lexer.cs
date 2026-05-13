using System;
using System.Collections.Generic;
using System.Text;

public enum TokenType
{
    Number,
    Identifier,
    Plus,
    Minus,
    Mul,
    Div,
    Mod,
    LParen,
    RParen,
    End
}

public class Token
{
    public TokenType Type { get; }
    public string Value { get; }
    public int Position { get; }

    public Token(TokenType type, string value, int position)
    {
        Type = type;
        Value = value;
        Position = position;
    }

    public override string ToString()
    {
        return $"{Type}: {Value}";
    }
}

public class Lab6ErrorInfo
{
    public string Fragment { get; }
    public int Position { get; }
    public int Length { get; }
    public string Description { get; }

    public Lab6ErrorInfo(string fragment, int position, int length, string description)
    {
        Fragment = fragment;
        Position = position;
        Length = length;
        Description = description;
    }
}

public class Lab6Lexer
{
    public List<string> Errors { get; } = new List<string>();
    public List<Lab6ErrorInfo> ErrorInfos { get; } = new List<Lab6ErrorInfo>();

    public List<Token> Analyze(string input)
    {
        Errors.Clear();
        ErrorInfos.Clear();

        List<Token> tokens = new List<Token>();

        if (input == null)
            input = "";

        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (char.IsDigit(c))
            {
                int start = i;
                StringBuilder sb = new StringBuilder();

                while (i < input.Length && char.IsDigit(input[i]))
                {
                    sb.Append(input[i]);
                    i++;
                }

                tokens.Add(new Token(TokenType.Number, sb.ToString(), start));
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                StringBuilder sb = new StringBuilder();

                while (i < input.Length &&
                       (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
                {
                    sb.Append(input[i]);
                    i++;
                }

                tokens.Add(new Token(TokenType.Identifier, sb.ToString(), start));
                continue;
            }

            switch (c)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus, "+", i));
                    break;

                case '-':
                    tokens.Add(new Token(TokenType.Minus, "-", i));
                    break;

                case '*':
                    tokens.Add(new Token(TokenType.Mul, "*", i));
                    break;

                case '/':
                    tokens.Add(new Token(TokenType.Div, "/", i));
                    break;

                case '%':
                    tokens.Add(new Token(TokenType.Mod, "%", i));
                    break;

                case '(':
                    tokens.Add(new Token(TokenType.LParen, "(", i));
                    break;

                case ')':
                    tokens.Add(new Token(TokenType.RParen, ")", i));
                    break;

                default:
                    string description = $"Лексическая ошибка: недопустимый символ '{c}'";

                    Errors.Add(description);

                    ErrorInfos.Add(new Lab6ErrorInfo(
                        c.ToString(),
                        i,
                        1,
                        description
                    ));

                    break;
            }

            i++;
        }

        tokens.Add(new Token(TokenType.End, "", input.Length));
        return tokens;
    }
}
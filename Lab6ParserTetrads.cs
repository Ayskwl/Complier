using System;
using System.Collections.Generic;

public class Tetrad
{
    public string Op { get; }
    public string Arg1 { get; }
    public string Arg2 { get; }
    public string Result { get; }

    public Tetrad(string op, string arg1, string arg2, string result)
    {
        Op = op;
        Arg1 = arg1;
        Arg2 = arg2;
        Result = result;
    }

    public override string ToString()
    {
        return $"({Op}, {Arg1}, {Arg2}, {Result})";
    }
}

public class Lab6ParserTetrads
{
    private List<Token> tokens;
    private int pos;
    private int tempCounter;

    private bool fatalSyntaxError;

    public List<string> Errors { get; } = new List<string>();
    public List<Lab6ErrorInfo> ErrorInfos { get; } = new List<Lab6ErrorInfo>();
    public List<Tetrad> Tetrads { get; } = new List<Tetrad>();

    private Token Current
    {
        get
        {
            if (tokens == null || tokens.Count == 0)
                return new Token(TokenType.End, "", 0);

            if (pos >= tokens.Count)
                return tokens[tokens.Count - 1];

            return tokens[pos];
        }
    }

    public bool Parse(List<Token> inputTokens)
    {
        tokens = inputTokens;
        pos = 0;
        tempCounter = 1;
        fatalSyntaxError = false;

        Errors.Clear();
        ErrorInfos.Clear();
        Tetrads.Clear();

        if (tokens == null || tokens.Count == 0)
        {
            AddError(
                new Token(TokenType.End, "", 0),
                "Синтаксическая ошибка: пустое выражение"
            );

            return false;
        }

        E();

        if (!fatalSyntaxError && Current.Type != TokenType.End)
        {
            if (Current.Type == TokenType.RParen)
            {
                AddError(
                    Current,
                    "Синтаксическая ошибка: лишняя закрывающая скобка"
                );
            }
            else
            {
                AddError(
                    Current,
                    $"Синтаксическая ошибка: лишний символ '{Current.Value}'"
                );
            }
        }

        if (Errors.Count > 0)
            Tetrads.Clear();

        return Errors.Count == 0;
    }

    private string E()
    {
        string left = T();

        if (fatalSyntaxError)
            return left;

        return A(left);
    }

    private string A(string left)
    {
        while (!fatalSyntaxError &&
               (Current.Type == TokenType.Plus ||
                Current.Type == TokenType.Minus))
        {
            string op = Current.Value;
            Move();

            if (!IsOperandStart(Current.Type))
            {
                AddError(
                    Current,
                    $"Синтаксическая ошибка: пропущен операнд после '{op}'"
                );

                fatalSyntaxError = true;
                SkipToEnd();
                return left;
            }

            string right = T();

            if (fatalSyntaxError)
                return left;

            string temp = NewTemp();

            Tetrads.Add(new Tetrad(op, left, right, temp));
            left = temp;
        }

        return left;
    }

    private string T()
    {
        string left = F();

        while (!fatalSyntaxError &&
               (Current.Type == TokenType.Mul ||
                Current.Type == TokenType.Div ||
                Current.Type == TokenType.Mod))
        {
            string op = Current.Value;
            Move();

            if (!IsOperandStart(Current.Type))
            {
                AddError(
                    Current,
                    $"Синтаксическая ошибка: пропущен операнд после '{op}'"
                );

                fatalSyntaxError = true;
                SkipToEnd();
                return left;
            }

            string right = F();

            if (fatalSyntaxError)
                return left;

            string temp = NewTemp();

            Tetrads.Add(new Tetrad(op, left, right, temp));
            left = temp;
        }

        return left;
    }

    private string F()
    {
        if (Current.Type == TokenType.Number ||
            Current.Type == TokenType.Identifier)
        {
            string value = Current.Value;
            Move();
            return value;
        }

        if (Current.Type == TokenType.LParen)
        {
            Move();

            if (Current.Type == TokenType.RParen)
            {
                AddError(
                    Current,
                    "Синтаксическая ошибка: внутри скобок отсутствует выражение"
                );

                fatalSyntaxError = true;
                SkipToEnd();
                return "";
            }

            string value = E();

            if (fatalSyntaxError)
                return value;

            if (Current.Type == TokenType.RParen)
            {
                Move();
            }
            else
            {
                AddError(
                    Current,
                    "Синтаксическая ошибка: пропущена закрывающая скобка"
                );

                fatalSyntaxError = true;
                SkipToEnd();
            }

            return value;
        }

        if (Current.Type == TokenType.RParen)
        {
            AddError(
                Current,
                "Синтаксическая ошибка: лишняя закрывающая скобка"
            );

            fatalSyntaxError = true;
            SkipToEnd();
            return "";
        }

        if (Current.Type == TokenType.End)
        {
            AddError(
                Current,
                "Синтаксическая ошибка: пропущен операнд"
            );

            fatalSyntaxError = true;
            return "";
        }

        AddError(
            Current,
            "Синтаксическая ошибка: пропущен операнд"
        );

        fatalSyntaxError = true;
        SkipToEnd();
        return "";
    }

    private void AddError(Token token, string description)
    {
        foreach (Lab6ErrorInfo existingError in ErrorInfos)
        {
            if (existingError.Position == token.Position &&
                existingError.Description == description)
            {
                return;
            }
        }

        string fragment;

        if (token.Type == TokenType.End)
            fragment = "(конец строки)";
        else
            fragment = token.Value;

        int length;

        if (token.Type == TokenType.End)
            length = 1;
        else
            length = Math.Max(token.Value.Length, 1);

        Errors.Add(description);

        ErrorInfos.Add(new Lab6ErrorInfo(
            fragment,
            token.Position,
            length,
            description
        ));
    }

    private void Move()
    {
        if (pos < tokens.Count - 1)
            pos++;
    }

    private void SkipToEnd()
    {
        while (Current.Type != TokenType.End)
        {
            Move();
        }
    }

    private string NewTemp()
    {
        return "t" + tempCounter++;
    }

    private bool IsOperandStart(TokenType type)
    {
        return type == TokenType.Number ||
               type == TokenType.Identifier ||
               type == TokenType.LParen;
    }
}
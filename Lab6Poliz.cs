using System;
using System.Collections.Generic;

public class Lab6Poliz
{
    public List<string> Errors { get; } = new List<string>();

    public bool CanBuildPoliz(List<Token> tokens)
    {
        Errors.Clear();

        foreach (Token token in tokens)
        {
            if (token.Type == TokenType.Identifier)
            {
                Errors.Add("ПОЛИЗ строится только для выражений, содержащих целые числа.");
                return false;
            }
        }

        return true;
    }

    public List<string> Build(List<Token> tokens)
    {
        Errors.Clear();

        List<string> output = new List<string>();
        Stack<Token> operations = new Stack<Token>();

        foreach (Token token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    output.Add(token.Value);
                    break;

                case TokenType.Plus:
                case TokenType.Minus:
                case TokenType.Mul:
                case TokenType.Div:
                case TokenType.Mod:
                    while (operations.Count > 0 &&
                           IsOperator(operations.Peek()) &&
                           Priority(operations.Peek()) >= Priority(token))
                    {
                        output.Add(operations.Pop().Value);
                    }

                    operations.Push(token);
                    break;

                case TokenType.LParen:
                    operations.Push(token);
                    break;

                case TokenType.RParen:
                    while (operations.Count > 0 &&
                           operations.Peek().Type != TokenType.LParen)
                    {
                        output.Add(operations.Pop().Value);
                    }

                    if (operations.Count > 0 &&
                        operations.Peek().Type == TokenType.LParen)
                    {
                        operations.Pop();
                    }

                    break;

                case TokenType.End:
                    break;
            }
        }

        while (operations.Count > 0)
        {
            Token operation = operations.Pop();

            if (operation.Type != TokenType.LParen &&
                operation.Type != TokenType.RParen)
            {
                output.Add(operation.Value);
            }
        }

        return output;
    }

    public int Calculate(List<string> poliz)
    {
        Stack<int> stack = new Stack<int>();

        foreach (string item in poliz)
        {
            if (int.TryParse(item, out int number))
            {
                stack.Push(number);
            }
            else
            {
                if (stack.Count < 2)
                    throw new Exception("Недостаточно операндов для операции.");

                int b = stack.Pop();
                int a = stack.Pop();

                switch (item)
                {
                    case "+":
                        stack.Push(a + b);
                        break;

                    case "-":
                        stack.Push(a - b);
                        break;

                    case "*":
                        stack.Push(a * b);
                        break;

                    case "/":
                        if (b == 0)
                            throw new DivideByZeroException();

                        stack.Push(a / b);
                        break;

                    case "%":
                        if (b == 0)
                            throw new DivideByZeroException();

                        stack.Push(a % b);
                        break;

                    default:
                        throw new Exception("Неизвестная операция: " + item);
                }
            }
        }

        if (stack.Count != 1)
            throw new Exception("Ошибка вычисления ПОЛИЗ.");

        return stack.Pop();
    }

    private bool IsOperator(Token token)
    {
        return token.Type == TokenType.Plus ||
               token.Type == TokenType.Minus ||
               token.Type == TokenType.Mul ||
               token.Type == TokenType.Div ||
               token.Type == TokenType.Mod;
    }

    private int Priority(Token token)
    {
        if (token.Type == TokenType.Mul ||
            token.Type == TokenType.Div ||
            token.Type == TokenType.Mod)
            return 2;

        if (token.Type == TokenType.Plus ||
            token.Type == TokenType.Minus)
            return 1;

        return 0;
    }
}
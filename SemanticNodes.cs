using System.Collections.Generic;
using System.Text;

namespace RGRCompilator
{
    public class SemanticError
    {
        public string Message { get; set; }
        public int Line { get; set; }
        public int Position { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }

        public string Location
        {
            get { return $"строка {Line}, символ {Position}"; }
        }
    }

    public class SemanticResult
    {
        public bool Success { get; set; }
        public List<SemanticError> Errors { get; set; } = new List<SemanticError>();
        public SemanticAstNode Root { get; set; }
        public string AstText { get; set; }

        public int ErrorCount
        {
            get { return Errors.Count; }
        }
    }

    public abstract class SemanticAstNode
    {
        public List<SemanticAstNode> Children { get; } = new List<SemanticAstNode>();
    }

    public class StructDeclNode : SemanticAstNode
    {
        public string Name { get; set; }

        public StructDeclNode(string name)
        {
            Name = name;
        }
    }

    public class FieldDeclNode : SemanticAstNode
    {
        public string FieldType { get; set; }
        public string Name { get; set; }

        public FieldDeclNode(string fieldType, string name)
        {
            FieldType = fieldType;
            Name = name;
        }
    }

    public static class SemanticAstPrinter
    {
        public static string Print(SemanticAstNode node)
        {
            if (node == null)
                return "Дерево не построено.";

            StringBuilder sb = new StringBuilder();
            PrintNode(node, sb, "", true);
            return sb.ToString();
        }

        private static void PrintNode(SemanticAstNode node, StringBuilder sb, string indent, bool isLast)
        {
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.AppendLine(GetCaption(node));

            string nextIndent = indent + (isLast ? "    " : "│   ");

            if (node is StructDeclNode structNode)
            {
                AppendLeaf(sb, nextIndent, false, $"Name: {structNode.Name}");

                for (int i = 0; i < structNode.Children.Count; i++)
                {
                    PrintNode(structNode.Children[i], sb, nextIndent, i == structNode.Children.Count - 1);
                }

                return;
            }

            if (node is FieldDeclNode fieldNode)
            {
                AppendLeaf(sb, nextIndent, false, $"Type: {fieldNode.FieldType}");
                AppendLeaf(sb, nextIndent, true, $"Name: {fieldNode.Name}");
                return;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                PrintNode(node.Children[i], sb, nextIndent, i == node.Children.Count - 1);
            }
        }

        private static void AppendLeaf(StringBuilder sb, string indent, bool isLast, string text)
        {
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");
            sb.AppendLine(text);
        }

        private static string GetCaption(SemanticAstNode node)
        {
            if (node is StructDeclNode)
                return "StructDecl";

            if (node is FieldDeclNode)
                return "FieldDecl";

            return "AstNode";
        }
    }
}
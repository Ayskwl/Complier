using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace RGRCompilator
{
    public partial class AstForm : Form
    {
        private readonly SemanticAstNode _root;
        private readonly Panel panelAst;
        private readonly PictureBox pictureBoxAst;

        public AstForm(SemanticAstNode root)
        {
            InitializeComponent();
            _root = root;
            Text = "Графическое представление";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            panelAst = new Panel();
            panelAst.Dock = DockStyle.Fill;
            panelAst.AutoScroll = true;
            Controls.Add(panelAst);

            pictureBoxAst = new PictureBox();
            pictureBoxAst.BackColor = Color.White;
            pictureBoxAst.Location = new Point(0, 0);
            pictureBoxAst.Size = new Size(3000, 2000);
            pictureBoxAst.Paint += pictureBoxAst_Paint;
            panelAst.Controls.Add(pictureBoxAst);
        }

        private void pictureBoxAst_Paint(object sender, PaintEventArgs e)
        {
            if (_root == null)
            {
                e.Graphics.DrawString("AST не построено", this.Font, Brushes.Black, 20, 20);
                return;
            }

            DrawNode(e.Graphics, _root, 600, 30, 250);
        }

        private void DrawNode(Graphics g, SemanticAstNode node, int x, int y, int offset)
        {
            string text = GetNodeText(node);

            Font font = new Font("Arial", 10);
            SizeF textSize = g.MeasureString(text, font);

            int boxWidth = (int)textSize.Width + 16;
            int boxHeight = (int)textSize.Height + 10;

            Rectangle rect = new Rectangle(x - boxWidth / 2, y, boxWidth, boxHeight);
            g.FillRectangle(Brushes.White, rect);
            g.DrawRectangle(Pens.Black, rect);
            g.DrawString(text, font, Brushes.Black, rect);

            List<SemanticAstNode> children = GetVisualChildren(node);

            if (children.Count == 0)
                return;

            int childY = y + 90;
            int startX = x - offset;
            int step = children.Count == 1 ? 0 : (offset * 2) / (children.Count - 1);

            for (int i = 0; i < children.Count; i++)
            {
                int childX = (children.Count == 1) ? x : startX + i * step;

                g.DrawLine(
                    Pens.Black,
                    x,
                    y + boxHeight,
                    childX,
                    childY
                );

                DrawNode(g, children[i], childX, childY, Math.Max(80, offset / 2));
            }
        }

        private List<SemanticAstNode> GetVisualChildren(SemanticAstNode node)
        {
            List<SemanticAstNode> result = new List<SemanticAstNode>();

            if (node is StructDeclNode structNode)
            {
                result.Add(new AstTextNode("Name", structNode.Name));

                foreach (var child in structNode.Children)
                    result.Add(child);

                return result;
            }

            if (node is FieldDeclNode fieldNode)
            {
                result.Add(new AstTextNode("Type", fieldNode.FieldType));
                result.Add(new AstTextNode("Name", fieldNode.Name));
                return result;
            }

            foreach (var child in node.Children)
                result.Add(child);

            return result;
        }

        private string GetNodeText(SemanticAstNode node)
        {
            if (node is StructDeclNode)
                return "StructDecl";

            if (node is FieldDeclNode)
                return "FieldDecl";

            if (node is AstTextNode textNode)
                return $"{textNode.Label}: {textNode.Value}";

            return "Node";
        }
    }

    public class AstTextNode : SemanticAstNode
    {
        public string Label { get; set; }
        public string Value { get; set; }

        public AstTextNode(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }
}
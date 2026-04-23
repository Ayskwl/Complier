using System.Drawing;
using System.Windows.Forms;

namespace RGRCompilator
{
    public partial class AstTextForm : Form
    {
        private readonly RichTextBox richTextBoxAst;

        public AstTextForm(string astText)
        {
            Text = "Дерево в текстовом виде";
            Width = 700;
            Height = 500;
            StartPosition = FormStartPosition.CenterScreen;

            richTextBoxAst = new RichTextBox();
            richTextBoxAst.Dock = DockStyle.Fill;
            richTextBoxAst.ReadOnly = true;
            richTextBoxAst.Font = new Font("Consolas", 10);
            richTextBoxAst.Text = astText;

            Controls.Add(richTextBoxAst);
        }
    }
}
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;

namespace RGRCompilator
{
    public partial class Complier : Form
    {
        private string currentFilePath = null;
        private bool isModified = false;
        private bool suppressModifiedFlag = false;
        private readonly Timer highlightTimer = new Timer();
        private readonly string[] keywords = new[]
        {
            "if","else","while","for","return","break","continue",
            "int","float","double","string","bool","void","char",
            "true","false","class","public","private","static","new"
        };
        public Complier()
        {
            InitializeComponent();
            toolStripButtonForward.Click += ToolStripMenuItemReplied_Click;
            toolStripButtonPaste.Click += ToolStripMenuItemPaste_Click;
            toolStripButtonDelete.Click += ToolStripMenuItemDelete_Click;
            toolStripButtonBack.Click += toolStripButtonBack_Click;
            toolStripButtonCopy.Click += toolStripButtonCopy_Click;
            toolStripButtonSave.Click += toolStripButtonSave_Click;
            toolStripButtonAdd.Click += toolStripButtonAdd_Click;
            toolStripButtonStart.Click += toolStripButtonStart_Click;

            rtbEditor.WordWrap = false;
            rtbEditor.ScrollBars = RichTextBoxScrollBars.Both;

            dgvResults.ReadOnly = true;
            dgvResults.AllowUserToAddRows = false;
            dgvResults.AllowUserToDeleteRows = false;
            dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResults.MultiSelect = false;

            highlightTimer.Interval = 250;
            highlightTimer.Tick += HighlightTimer_Tick;

            rtbEditor.TextChanged += rtbEditor_TextChanged;
            this.FormClosing += Complier_FormClosing;

            dgvResults.CellDoubleClick += dgvResults_CellContentClick;
            EnsureResultsColumns();
            UpdateFormTitle();
        }
        private void OpenFile()
        {
            if (!ConfirmSaveIfNeeded("Открыть файл"))
                return;

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Открыть файл";
                ofd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = ofd.FileName;
                    string text = File.ReadAllText(currentFilePath, Encoding.UTF8);

                    suppressModifiedFlag = true;
                    rtbEditor.Text = text;
                    suppressModifiedFlag = false;

                    isModified = false;
                    UpdateFormTitle();

                    dgvResults.Rows.Clear();
                    StartHighlightDelayed();
                }
            }
        }
        private void NewFile()
        {
            if (!ConfirmSaveIfNeeded("Создать новый файл"))
                return;

            suppressModifiedFlag = true;
            rtbEditor.Clear();
            suppressModifiedFlag = false;

            currentFilePath = null;
            isModified = false;
            UpdateFormTitle();
            dgvResults.Rows.Clear();
        }
       

        private bool SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                currentFilePath = Path.Combine(appFolder, "code.txt");
            }

            File.WriteAllText(currentFilePath, rtbEditor.Text, Encoding.UTF8);

            isModified = false;
            UpdateFormTitle();

            return true;
        }

        private bool SaveFileAs()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Сохранить как";
                sfd.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                if (!string.IsNullOrEmpty(currentFilePath))
                    sfd.FileName = Path.GetFileName(currentFilePath);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                    File.WriteAllText(currentFilePath, rtbEditor.Text, Encoding.UTF8);
                    isModified = false;
                    UpdateFormTitle();
                    return true;
                }
            }
            return false;
        }
        private bool ConfirmSaveIfNeeded(string caption)
        {
            if (!isModified) return true;

            var result = MessageBox.Show(
                "Сохранить изменения в документе?",
                caption,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
                return false;

            if (result == DialogResult.Yes)
                return SaveFile();

            return true; 
        }
        private void UpdateFormTitle()
        {
            string name = string.IsNullOrEmpty(currentFilePath) ? "Безымянный" : Path.GetFileName(currentFilePath);
            this.Text = "Compiler - " + name + (isModified ? "*" : "");
        }
        private void StartHighlightDelayed()
        {
            highlightTimer.Stop();
            highlightTimer.Start();
        }

        private void HighlightTimer_Tick(object sender, EventArgs e)
        {
            highlightTimer.Stop();
            ApplySyntaxHighlighting();
        }
        private void ApplySyntaxHighlighting()
        {
            string text = rtbEditor.Text;
            if (string.IsNullOrEmpty(text)) return;

            int selStart = rtbEditor.SelectionStart;
            int selLen = rtbEditor.SelectionLength;

            suppressModifiedFlag = true;
            rtbEditor.SuspendLayout();

            rtbEditor.SelectAll();
            rtbEditor.SelectionColor = Color.Black;
            rtbEditor.SelectionFont = new Font(rtbEditor.Font, FontStyle.Regular);

            foreach (Match m in Regex.Matches(text, @"//.*?$", RegexOptions.Multiline))
            {
                rtbEditor.Select(m.Index, m.Length);
                rtbEditor.SelectionColor = Color.Green;
            }

            foreach (Match m in Regex.Matches(text, "\"(?:\\\\.|[^\"\\\\])*\""))
            {
                rtbEditor.Select(m.Index, m.Length);
                rtbEditor.SelectionColor = Color.Brown;
            }

            foreach (Match m in Regex.Matches(text, @"\b\d+(\.\d+)?\b"))
            {
                rtbEditor.Select(m.Index, m.Length);
                rtbEditor.SelectionColor = Color.DarkCyan;
            }

            string kwPattern = $@"\b({string.Join("|", keywords.Select(Regex.Escape))})\b";
            foreach (Match m in Regex.Matches(text, kwPattern))
            {
                rtbEditor.Select(m.Index, m.Length);
                rtbEditor.SelectionColor = Color.Blue;
                rtbEditor.SelectionFont = new Font(rtbEditor.Font, FontStyle.Bold);
            }

            rtbEditor.Select(selStart, selLen);

            rtbEditor.ResumeLayout();
            suppressModifiedFlag = false;
        }
        private void EnsureResultsColumns()
        {
            if (dgvResults.Columns.Count > 0) return;

            dgvResults.Columns.Add("colType", "Тип");
            dgvResults.Columns.Add("colMessage", "Сообщение");
            dgvResults.Columns.Add("colLine", "Строка");
            dgvResults.Columns.Add("colCol", "Символ");

            int cStart = dgvResults.Columns.Add("colStart", "StartIndex");
            dgvResults.Columns[cStart].Visible = false;

            int cLen = dgvResults.Columns.Add("colLen", "Length");
            dgvResults.Columns[cLen].Visible = false;
        }
        private void AddError(string message, int startIndex, int length)
        {
            EnsureResultsColumns();

            (int line, int col) = GetLineColFromIndex(rtbEditor.Text, startIndex);

            dgvResults.Rows.Add(
                "Ошибка",
                message,
                line.ToString(),
                col.ToString(),
                startIndex,
                length);
        }
        private void AddInfo(string message)
        {
            EnsureResultsColumns();
            dgvResults.Rows.Add("Инфо", message, "", "", -1, 0);
        }
        private void Complier_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmSaveIfNeeded("Выход"))
                e.Cancel = true;
        }
        private void RunAnalyzer()
        {
            dgvResults.Rows.Clear();
            EnsureResultsColumns();

            string code = rtbEditor.Text ?? "";

            if (code.Length == 0)
            {
                AddInfo("Пустой текст.");
                return;
            }

            int quoteStart = -1;
            char quoteChar = '\0';
            bool escaped = false;
            for (int i = 0; i < code.Length; i++)
            {
                char ch = code[i];

                if (ch == '\\' && !escaped)
                {
                    escaped = true;
                    continue;
                }

                if ((ch == '"' || ch == '\'')&& !escaped)
                {
                    if (quoteStart == -1)
                    {
                        quoteStart = i;
                        quoteChar = ch;
                    }
                    else if (ch == quoteChar)
                    {
                        quoteStart = -1;
                        quoteChar = '\0';
                    }
                }

                escaped = false;
            }
            if (quoteStart != -1)
            {
                AddError(
                     $"Незакрытая строка (нет закрывающей кавычки {quoteChar})",
                     quoteStart,
                     1);
            }

            var stack = new System.Collections.Generic.Stack<int>();
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '(') stack.Push(i);
                if (code[i] == ')')
                {
                    if (stack.Count == 0)
                        AddError("Лишняя закрывающая скобка )", i, 1);
                    else
                        stack.Pop();
                }
            }
            while (stack.Count > 0)
            {
                int pos = stack.Pop();
                AddError("Нет закрывающей скобки )", pos, 1);
            }

            string allowed = " \t\r\n_+-*/=;:,.(){}[]<>!&|\"'\\";
            for (int i = 0; i < code.Length; i++)
            {
                char ch = code[i];

                bool ok =
                    char.IsLetterOrDigit(ch) ||
                    allowed.IndexOf(ch) >= 0;

                if (!ok)
                {
                    AddError($"Недопустимый символ: '{ch}'", i, 1);
                }
            }

            bool hasErrors = dgvResults.Rows
                .Cast<DataGridViewRow>()
                .Any(r => (r.Cells["colType"].Value?.ToString() ?? "") == "Ошибка");

            if (!hasErrors)
            {
                int tokenCount = Regex.Matches(code, @"[A-Za-zА-Яа-я_]\w*|\d+(\.\d+)?|""(?:\\.|[^""\\])*""|[^\s]")
                                     .Count;

                AddInfo("Анализ завершён успешно.");
            }
            else
            {
                AddInfo("Анализ завершён с ошибками. Двойной клик по ошибке — перейти к месту.");
            }
        }
        private void ToolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        
        private void toolStripButtonAdd_Click(object sender, EventArgs e)
        {
            NewFile();
        }

        private void ToolStripMenuItemAdd_Click(object sender, EventArgs e)
        {
            NewFile();
        }
        private void rtbEditor_TextChanged(object sender, EventArgs e)
        {
            if (suppressModifiedFlag) return;

            isModified = true;
            UpdateFormTitle();
            StartHighlightDelayed();
        }
        private void ToolStripMenuItemSave_Click(object sender, EventArgs e)
        {
            SaveFile();
        }

        private void ToolStripMenuItemSaveAs_Click(object sender, EventArgs e)
        {
            SaveFileAs();
        }
        private void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            SaveFile();
        }
        private void ToolStripMenuItemAboutProgram_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Компилятор (текстовый редактор для языкового процессора)\n\n" +
                "Версия: 1.0\n" +
                "Разработчик: Студент группы АП-326 Бабаева Дарья\n" +
                "© 2026\n\n" +
                "Программа представляет собой текстовый редактор с графическим\n" +
                "интерфейсом, предназначенный для ввода и редактирования исходного текста\n" +
                "и отображения результатов работы языкового процессора.",
                "О программе",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        private void toolStripButtonInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Компилятор (текстовый редактор для языкового процессора)\n\n" +
                "Версия: 1.0\n" +
                "Разработчик: Студент группы АП-326 Бабаева Дарья\n" +
                "© 2026\n\n" +
                "Программа представляет собой текстовый редактор с графическим\n" +
                "интерфейсом, предназначенный для ввода и редактирования исходного текста\n" +
                "и отображения результатов работы языкового процессора.",
                "О программе",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ToolStripButtonDelete_Click(object sender, EventArgs e)
        {
            if (rtbEditor.SelectionLength > 0)
                rtbEditor.SelectedText = string.Empty;
        }

        private void ToolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e)
        {
            OpenFile();
        }
        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            RunAnalyzer();
        }

        private void ToolStripMenuItemStart_Click(object sender, EventArgs e)
        {
            RunAnalyzer();
        }

        private void ToolStripMenuItemCancel_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo)
                rtbEditor.Undo();
        }
        private void toolStripButtonBack_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo)
                rtbEditor.Undo();
        }
        private void toolStripButtonForward_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo)
                rtbEditor.Redo();
        }
        private void ToolStripMenuItemReplied_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo)
                rtbEditor.Redo();
        }
        
        private void ToolStripMenuItemCut_Click(object sender, EventArgs e)
        {
            rtbEditor.Cut();
        }
        private void toolStripButtonDelete_Click_1(object sender, EventArgs e)
        {
            rtbEditor.Cut();
        }
        private void ToolStripMenuItemCopy_Click(object sender, EventArgs e)
        {
            rtbEditor.Copy();
        }

        private void ToolStripMenuItemPaste_Click(object sender, EventArgs e)
        {
            rtbEditor.Paste();
        }

        private void ToolStripMenuItemDelete_Click(object sender, EventArgs e)
        {
            if (rtbEditor.SelectionLength > 0)
                rtbEditor.SelectedText = string.Empty;
        }

        private void ToolStripMenuItemSelectAll_Click(object sender, EventArgs e)
        {
            rtbEditor.SelectAll();
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e)
        {
            rtbEditor.Copy();
        }


        private void dgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvResults.Rows[e.RowIndex];
            if (row.Cells["colStart"].Value == null) return;

            int start = Convert.ToInt32(row.Cells["colStart"].Value);
            int len = Convert.ToInt32(row.Cells["colLen"].Value);

            if (start < 0 || start > rtbEditor.TextLength) return;

            rtbEditor.Focus();
            rtbEditor.SelectionStart = start;
            rtbEditor.SelectionLength = Math.Max(0, Math.Min(len, rtbEditor.TextLength - start));
            rtbEditor.ScrollToCaret();
        }
        private (int line, int col) GetLineColFromIndex(string text, int index)
        {
            if (index < 0) return (0, 0);
            if (index > text.Length) index = text.Length;

            int line = 1;
            int col = 1;

            for (int i = 0; i < index; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    col = 1;
                }
                else
                {
                    col++;
                }
            }
            return (line, col);
        }

        private void ShowUserGuide()
        {
            MessageBox.Show(
                "РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ\n\n" +

                "1. Область редактирования (верхнее окно):\n" +
                "   - Предназначена для ввода и редактирования исходного текста.\n" +
                "   - Поддерживается подсветка синтаксиса.\n\n" +

                "2. Область результатов (нижнее окно):\n" +
                "   - Отображает сообщения и результаты работы языкового процессора.\n" +
                "   - Двойной щелчок по ошибке перемещает курсор к месту ошибки.\n\n" +

                "3. Меню 'Файл':\n" +
                "   - Создать — очистить редактор и создать новый документ.\n" +
                "   - Открыть — загрузить текстовый файл.\n" +
                "   - Сохранить — сохранить текущий файл.\n" +
                "   - Сохранить как — сохранить файл под новым именем.\n" +
                "   - Выход — закрыть программу (при необходимости предлагается сохранить изменения).\n\n" +

                "4. Меню 'Правка':\n" +
                "   - Отменить / Повторить — отмена или возврат действий.\n" +
                "   - Вырезать / Копировать / Вставить — работа с буфером обмена.\n" +
                "   - Удалить — удалить выделенный текст.\n" +
                "   - Выделить всё — выделить весь текст.\n\n" +

                "5. Кнопка 'Пуск':\n" +
                "   - Запускает анализ текста.\n" +
                "   - Результаты отображаются в области результатов.\n\n" +

                "6. Дополнительно:\n" +
                "   - При изменении текста программа предлагает сохранить изменения.\n" +
                "   - В процессе реализации пункт меню: текст.\n\n" +

                "Версия 1.0\n" +
                "Разработчик: Бабаева Дарья",
                "Руководство пользователя",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        private void ToolStripMenuItemUserGuide_Click(object sender, EventArgs e)
        {
            ShowUserGuide();
        }

        private void toolStripButtonRefs_Click(object sender, EventArgs e)
        {
            ShowUserGuide();
        }

        
    }
}

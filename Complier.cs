using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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

        public Complier()
        {
            InitializeComponent();

            toolStripButtonForward.Click += toolStripButtonForward_Click;
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

            rtbEditor.TextChanged += rtbEditor_TextChanged;
            this.FormClosing += Complier_FormClosing;


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

        private void EnsureResultsColumns()
        {
            dgvResults.Columns.Clear();

            dgvResults.Columns.Add("colFragment", "Неверный фрагмент");
            dgvResults.Columns.Add("colLocation", "Местоположение");
            dgvResults.Columns.Add("colDescription", "Описание");

            int cStart = dgvResults.Columns.Add("colStart", "StartIndex");
            dgvResults.Columns[cStart].Visible = false;

            int cLen = dgvResults.Columns.Add("colLen", "Length");
            dgvResults.Columns[cLen].Visible = false;

            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void FillLexicalErrorsTable(List<Lexeme> lexemes)
        {
            dgvResults.Rows.Clear();
            EnsureResultsColumns();

            var lexicalErrors = lexemes.Where(x => x.IsError).ToList();

            foreach (var lex in lexicalErrors)
            {
                dgvResults.Rows.Add(
                    lex.Value,
                    $"строка {lex.Line}, позиция {lex.StartColumn}",
                    lex.ErrorMessage,
                    lex.StartIndex,
                    lex.Length
                );
            }

            dgvResults.Rows.Add(
                "Общее количество ошибок:",
                "",
                lexicalErrors.Count.ToString(),
                -1,
                0
            );
        }

        private void FillSyntaxErrorsTable(ParserResult result)
        {
            dgvResults.Rows.Clear();
            EnsureResultsColumns();

            foreach (var err in result.Errors)
            {
                dgvResults.Rows.Add(
                    err.InvalidFragment,
                    err.Location,
                    err.Description,
                    err.StartIndex,
                    err.Length
                );
            }

            dgvResults.Rows.Add(
                "Общее количество ошибок:",
                "",
                result.Errors.Count.ToString(),
                -1,
                0
            );
        }

        private void AddInfo(string message)
        {
            dgvResults.Rows.Clear();
            EnsureResultsColumns();
            dgvResults.Rows.Add("", "", message, -1, 0);
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

            var analyzer = new LexicalAnalyzer();
            List<Lexeme> lexemes = analyzer.Analyze(code);

            var lexicalErrors = lexemes.Where(x => x.IsError).ToList();

            if (lexicalErrors.Count > 0)
            {
                FillLexicalErrorsTable(lexemes);
                return;
            }

            var parser = new SyntaxParser(lexemes);
            ParserResult result = parser.Parse();

            if (result.Success)
            {
                AddInfo("Синтаксических ошибок не обнаружено.");
            }
            else
            {
                FillSyntaxErrorsTable(result);
            }
        }

        private void toolStripButtonAdd_Click(object sender, EventArgs e) => NewFile();

        private void ToolStripMenuItemAdd_Click(object sender, EventArgs e) => NewFile();

        private void rtbEditor_TextChanged(object sender, EventArgs e)
        {
            if (suppressModifiedFlag) return;
            isModified = true;
            UpdateFormTitle();
        }

        private void ToolStripMenuItemSave_Click(object sender, EventArgs e) => SaveFile();

        private void ToolStripMenuItemSaveAs_Click(object sender, EventArgs e) => SaveFileAs();

        private void toolStripButtonSave_Click(object sender, EventArgs e) => SaveFile();

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

        private void toolStripButtonInfo_Click(object sender, EventArgs e) => ToolStripMenuItemAboutProgram_Click(sender, e);

        private void ToolStripMenuItemOpen_Click(object sender, EventArgs e) => OpenFile();

        private void toolStripButtonOpen_Click(object sender, EventArgs e) => OpenFile();

        private void toolStripButtonStart_Click(object sender, EventArgs e) => RunAnalyzer();

        private void ToolStripMenuItemStart_Click(object sender, EventArgs e) => RunAnalyzer();

        private void ToolStripMenuItemExit_Click(object sender, EventArgs e) => this.Close();

        private void ToolStripMenuItemCancel_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo) rtbEditor.Undo();
        }

        private void toolStripButtonBack_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanUndo) rtbEditor.Undo();
        }

        private void toolStripButtonForward_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanRedo) rtbEditor.Redo();
        }

        private void ToolStripMenuItemReplied_Click(object sender, EventArgs e)
        {
            if (rtbEditor.CanRedo) rtbEditor.Redo();
        }

        private void ToolStripMenuItemCut_Click(object sender, EventArgs e) => rtbEditor.Cut();

        private void ToolStripMenuItemCopy_Click(object sender, EventArgs e) => rtbEditor.Copy();

        private void toolStripButtonCopy_Click(object sender, EventArgs e) => rtbEditor.Copy();

        private void ToolStripMenuItemPaste_Click(object sender, EventArgs e) => rtbEditor.Paste();

        private void ToolStripMenuItemDelete_Click(object sender, EventArgs e)
        {
            if (rtbEditor.SelectionLength > 0)
                rtbEditor.SelectedText = string.Empty;
        }

        private void ToolStripMenuItemSelectAll_Click(object sender, EventArgs e) => rtbEditor.SelectAll();

        private void dgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvResults.Rows[e.RowIndex];

            if (row.Cells["colStart"].Value == null || row.Cells["colLen"].Value == null)
                return;

            int start = Convert.ToInt32(row.Cells["colStart"].Value);
            int len = Convert.ToInt32(row.Cells["colLen"].Value);

            if (start < 0 || start > rtbEditor.TextLength)
                return;

            rtbEditor.Focus();
            rtbEditor.SelectionStart = start;
            rtbEditor.SelectionLength = Math.Max(1, Math.Min(len, rtbEditor.TextLength - start));
            rtbEditor.ScrollToCaret();
        }

        

        private void ShowUserGuide()
        {
            MessageBox.Show(
                "РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ\n\n" +
                "1. Область редактирования (верхнее окно):\n" +
                "   - Предназначена для ввода и редактирования исходного текста.\n\n" +
                "2. Область результатов (нижнее окно):\n" +
                "   - Отображает сообщения и результаты работы языкового процессора.\n" +
                "   - Двойной щелчок по ошибке перемещает курсор к месту ошибки.\n\n" +
                "3. Меню 'Файл':\n" +
                "   - Создать — новый документ.\n" +
                "   - Открыть — загрузить текстовый файл.\n" +
                "   - Сохранить — сохранить текущий файл.\n" +
                "   - Сохранить как — сохранить файл под новым именем.\n" +
                "   - Выход — закрыть программу.\n\n" +
                "4. Меню 'Правка':\n" +
                "   - Отменить / Повторить.\n" +
                "   - Вырезать / Копировать / Вставить.\n" +
                "   - Удалить выделенный текст.\n" +
                "   - Выделить всё.\n\n" +
                "5. Кнопка 'Пуск':\n" +
                "   - Запускает анализ текста.\n\n" +
                "Версия 1.0\n" +
                "Разработчик: Бабаева Дарья",
                "Руководство пользователя",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        
        private void ToolStripMenuItemUserGuide_Click(object sender, EventArgs e) => ShowUserGuide();

        private void toolStripButtonRefs_Click(object sender, EventArgs e) => ShowUserGuide();
    }
}

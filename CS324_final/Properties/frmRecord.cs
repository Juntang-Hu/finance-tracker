using System;
using System.Drawing;
using System.Windows.Forms;

namespace AccountingApp
{
    /// <summary>新增 / 編輯記帳紀錄的對話視窗</summary>
    public class frmRecord : Form
    {
        /// <summary>使用者確認後傳回的結果物件</summary>
        public RecordItem Result { get; private set; }

        private DateTimePicker dtp;
        private ComboBox       cmbType, cmbCat;
        private TextBox        txtDesc, txtAmt;
        private Label          lblError;
        private Button         btnOK, btnCancel;

        private readonly RecordItem _editing;   // null = 新增；有值 = 編輯

        // ══════════════════════════════════════════════════════════
        // 建構子
        // ══════════════════════════════════════════════════════════
        public frmRecord(RecordItem editItem)
        {
            _editing = editItem;
            BuildUI();
            if (editItem != null) LoadForEdit(editItem);
        }

        // ══════════════════════════════════════════════════════════
        // 建立介面
        // ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text            = _editing == null ? "➕ 新增記帳" : "✏️ 編輯記帳";
            this.Size            = new Size(380, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.Font            = new Font("微軟正黑體", 10f);
            this.BackColor       = Color.White;

            const int LX = 18, FX = 95, FW = 248, DY = 46, Y0 = 18;

            // ── 日期 ──────────────────────────────────────────────
            this.Controls.Add(Row("日期：", LX, Y0));
            dtp = new DateTimePicker
            {
                Left = FX, Top = Y0 - 2, Width = FW,
                Format = DateTimePickerFormat.Short
            };
            this.Controls.Add(dtp);

            // ── 收支 ──────────────────────────────────────────────
            this.Controls.Add(Row("收支：", LX, Y0 + DY));
            cmbType = new ComboBox
            {
                Left = FX, Top = Y0 + DY - 2, Width = FW,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new[] { "支出", "收入" });
            cmbType.SelectedIndex = 0;
            this.Controls.Add(cmbType);

            // ── 類別（在訂閱 cmbType 事件前先建立好 cmbCat）─────
            this.Controls.Add(Row("類別：", LX, Y0 + DY * 2));
            cmbCat = new ComboBox
            {
                Left = FX, Top = Y0 + DY * 2 - 2, Width = FW,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cmbCat);
            // cmbCat 建好後再訂閱事件，避免事件觸發時 cmbCat 是 null
            cmbType.SelectedIndexChanged += (s, e) => RefreshCategories();
            RefreshCategories();    // 初始化類別清單

            // ── 描述 ──────────────────────────────────────────────
            this.Controls.Add(Row("描述：", LX, Y0 + DY * 3));
            txtDesc = new TextBox
            {
                Left = FX, Top = Y0 + DY * 3 - 2, Width = FW,
            };
            this.Controls.Add(txtDesc);

            // ── 金額 ──────────────────────────────────────────────
            this.Controls.Add(Row("金額：", LX, Y0 + DY * 4));
            txtAmt = new TextBox
            {
                Left = FX, Top = Y0 + DY * 4 - 2, Width = FW,
            };
            txtAmt.TextChanged += (s, e) =>
            {
                lblError.Text        = "";
                txtAmt.BackColor     = Color.White;
            };
            this.Controls.Add(txtAmt);

            // ── 錯誤訊息 ─────────────────────────────────────────
            lblError = new Label
            {
                Left      = FX, Top = Y0 + DY * 5 - 8,
                Width     = FW, AutoSize = false,
                ForeColor = Color.Crimson,
                Font      = new Font("微軟正黑體", 9f)
            };
            this.Controls.Add(lblError);

            // ── 確認 / 取消 ──────────────────────────────────────
            btnOK = new Button
            {
                Text      = "✔ 確認", Width = 100, Height = 34,
                Left      = 100, Top = 265,
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text      = "✘ 取消", Width = 100, Height = 34,
                Left      = 214, Top = 265,
                BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        // ── 類別下拉（依收 / 支切換）─────────────────────────────
        private void RefreshCategories()
        {
            if (cmbCat == null) return;
            cmbCat.Items.Clear();
            bool isIncome = cmbType.SelectedIndex == 1;
            cmbCat.Items.AddRange(isIncome ? Categories.Income : Categories.Expense);
            cmbCat.SelectedIndex = 0;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // frmRecord
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "frmRecord";
            this.Load += new System.EventHandler(this.frmRecord_Load);
            this.ResumeLayout(false);

        }

        private void frmRecord_Load(object sender, EventArgs e)
        {

        }

        // ── 載入既有紀錄（編輯模式）──────────────────────────────
        private void LoadForEdit(RecordItem item)
        {
            dtp.Value            = item.Date;
            cmbType.SelectedItem = item.Type;
            RefreshCategories();
            cmbCat.SelectedItem  = item.Category;
            if (cmbCat.SelectedIndex < 0) cmbCat.SelectedIndex = 0;
            txtDesc.Text = item.Description;
            txtAmt .Text = item.Amount.ToString("F0");
        }

        // ── 確認按鈕（含驗證）────────────────────────────────────
        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!double.TryParse(txtAmt.Text, out double amt) || amt <= 0)
            {
                lblError.Text        = "⚠ 請輸入大於 0 的數字！";
                txtAmt.BackColor     = Color.FromArgb(255, 220, 220);
                txtAmt.Focus();
                return;
            }

            Result = new RecordItem
            {
                Id          = _editing?.Id ?? 0,
                Date        = dtp.Value.Date,
                Category    = cmbCat.SelectedItem?.ToString() ?? "",
                Description = txtDesc.Text.Trim(),
                Amount      = amt,
                Type        = cmbType.SelectedItem?.ToString() ?? "支出"
            };
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // ── Helper ───────────────────────────────────────────────
        private static Label Row(string text, int left, int top) =>
            new Label
            {
                Text      = text,
                AutoSize  = true,
                Left      = left, Top = top + 4,
                ForeColor = Color.FromArgb(44, 62, 80)
            };
    }
}

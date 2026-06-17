using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;


namespace AccountingApp
{
    public class MainForm : Form
    {
        // ── 資料層 ────────────────────────────────────────────────
        private readonly RecordManager _manager = new RecordManager();
        private List<RecordItem> _current = new List<RecordItem>();

        // ── 控制項 ────────────────────────────────────────────────
        private MenuStrip               menuStrip;
        private Panel                   pnlFilter, pnlSummary, pnlButtons;
        private ComboBox                cmbYear, cmbMonth;
        private Label                   lblIncome, lblExpense, lblBalance;
        private DataGridView            dgv;
        private StatusStrip             statusStrip;
        private ToolStripStatusLabel    tsslInfo;
        private NotifyIcon              notifyIcon;
        private System.Windows.Forms.Timer timerReminder;
        private DateTime _lastRemindDate = DateTime.MinValue;

        private static readonly Dictionary<string, Color> CategoryColors =
        new Dictionary<string, Color>
        {
            // 支出
            { "餐飲",   Color.FromArgb(255, 224, 178) },
            { "交通",   Color.FromArgb(187, 222, 251) },
            { "住宿",   Color.FromArgb(225, 190, 231) },
            { "娛樂",   Color.FromArgb(255, 205, 210) },
            { "購物",   Color.FromArgb(255, 249, 196) },
            { "醫療",   Color.FromArgb(200, 230, 201) },
            { "教育",   Color.FromArgb(178, 235, 242) },
            { "通訊",   Color.FromArgb(207, 216, 220) },
            { "日用品", Color.FromArgb(239, 235, 233) },
            { "其他支出", Color.FromArgb(220, 220, 220) },
            // 收入
            { "薪資",   Color.FromArgb(165, 214, 167) },
            { "獎金",   Color.FromArgb(255, 236, 179) },
            { "投資",   Color.FromArgb(128, 222, 234) },
            { "兼職",   Color.FromArgb(159, 168, 218) },
            { "禮金",   Color.FromArgb(248, 187, 208) },
            { "其他收入", Color.FromArgb(210, 210, 210) },
        };
        private TextBox txtSearch;
        private System.Windows.Forms.Timer timerBg;
        private int _hue = 0;

        // ══════════════════════════════════════════════════════════
        // 建構子
        // ══════════════════════════════════════════════════════════
        public MainForm()
        {
            BuildUI();
            _manager.Load();
            FillYearCombo();
            Refresh_();
            StartReminder();
            // 優先用內嵌資源，若使用者有自選的路徑則用自選的
            string saved = AudioHelper.LoadBgmPath();
            if (!string.IsNullOrEmpty(saved) && File.Exists(saved))
                AudioHelper.StartBgm(saved);
            else
                AudioHelper.StartBgmFromResource();
        }

        // ══════════════════════════════════════════════════════════
        // 建立介面
        // ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text          = "💰 個人記帳系統";
            this.Size          = new Size(960, 640);
            this.MinimumSize   = new Size(780, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font          = new Font("微軟正黑體", 9.5f);
            this.BackColor     = Color.White;
            this.FormClosing  += (s, e) => { notifyIcon?.Dispose(); AudioHelper.StopBgm(); };

            BuildMenuStrip();
            BuildFilterPanel();
            BuildSummaryPanel();
            BuildButtonPanel();
            BuildDataGridView();
            BuildStatusStrip();
            BuildNotifyIcon();

            // ★ Dock 的視覺位置由 Controls.Add 的順序決定：
            //   最後 Add 的 DockStyle.Top → 排在最上面
            this.SuspendLayout();
            this.Controls.Add(dgv);         // Fill  ─ 最先 Add
            this.Controls.Add(pnlButtons);  // Right
            this.Controls.Add(statusStrip); // Bottom
            this.Controls.Add(pnlSummary);  // Top（視覺第 3 排）
            this.Controls.Add(pnlFilter);   // Top（視覺第 2 排）
            this.Controls.Add(menuStrip);   // Top（視覺最頂，最後 Add）
            this.MainMenuStrip = menuStrip;
            this.ResumeLayout();
        }

        //bgcolor
        // 套用背景色到所有面板
        private void ApplyBgColor(Color c)
        {
            pnlFilter.BackColor = c;
            pnlSummary.BackColor = c;
            pnlButtons.BackColor = c;
            statusStrip.BackColor = c;
        }

        // 開始漸變
        private void StartGradient()
        {
            if (timerBg == null)
            {
                timerBg = new System.Windows.Forms.Timer { Interval = 30 };
                timerBg.Tick += (s, e) =>
                {
                    _hue = (_hue + 1) % 360;
                    ApplyBgColor(HsvToRgb(_hue, 0.25f, 0.92f));  // 淡色系漸變
                };
            }
            timerBg.Start();
        }

        // 停止漸變
        private void StopGradient() => timerBg?.Stop();

        // HSV 轉 RGB（讓顏色循環平滑）
        private static Color HsvToRgb(int hue, float sat, float val)
        {
            float h = hue / 60f;
            int i = (int)h;
            float f = h - i;
            float p = val * (1 - sat);
            float q = val * (1 - sat * f);
            float t = val * (1 - sat * (1 - f));
            float r, g, b;
            switch (i % 6)
            {
                case 0: r = val; g = t; b = p; break;
                case 1: r = q; g = val; b = p; break;
                case 2: r = p; g = val; b = t; break;
                case 3: r = p; g = q; b = val; break;
                case 4: r = t; g = p; b = val; break;
                default: r = val; g = p; b = q; break;
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        // ── MenuStrip ──────────────────────────────────────────────
        private void BuildMenuStrip()
        {
            menuStrip = new MenuStrip { BackColor = Color.FromArgb(44, 62, 80), Renderer = new ToolStripProfessionalRenderer() };

            // 檔案
            var mFile   = MakeMenu("檔案(&F)");
            var mExport = new ToolStripMenuItem("匯出 CSV(&E)"); mExport.Click += OnExport;
            var mImport = new ToolStripMenuItem("匯入 CSV(&I)"); mImport.Click += OnImport;
            var mExit = new ToolStripMenuItem("結束(&X)"); mExit.Click += (s, e) => System.Windows.Forms.Application.Exit();
            mFile.DropDownItems.AddRange(new ToolStripItem[] { mImport, mExport, new ToolStripSeparator(), mExit });



            // 功能
            var mView  = MakeMenu("功能(&V)");
            var mAdd   = new ToolStripMenuItem("新增紀錄(&N)"); mAdd.Click   += OnAdd;
            var mChart = new ToolStripMenuItem("查看圖表(&C)"); mChart.Click += OnChart;
            mView.DropDownItems.AddRange(new ToolStripItem[] { mAdd, mChart });

            // 說明
            var mHelp  = MakeMenu("說明(&H)");
            var mAbout = new ToolStripMenuItem("關於");
            mAbout.Click += (s, e) => MessageBox.Show(
                "個人記帳系統 v1.0\n雙擊紀錄可快速編輯",
                "關於", MessageBoxButtons.OK, MessageBoxIcon.Information);
            mHelp.DropDownItems.Add(mAbout);

            //music
            var mMusic = MakeMenu("音樂(&M)");
            var mToggle = new ToolStripMenuItem("暫停 / 繼續 BGM");
            mToggle.Click += (s, e) =>
            {
                AudioHelper.ToggleBgm();
                mToggle.Text = AudioHelper.IsBgmOn ? "暫停 / 繼續 BGM" : "▶ 繼續 BGM";
            };

            var mSelect = new ToolStripMenuItem("選擇 BGM 檔案...");   // ← 新增
            mSelect.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog
                {
                    Title = "選擇背景音樂",
                    Filter = "音樂檔案|*.mp3;*.wav;*.wma|所有檔案|*.*"
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        AudioHelper.SaveBgmPath(ofd.FileName);  // 記住路徑
                        AudioHelper.StopBgm();
                        AudioHelper.StartBgm(ofd.FileName);     // 馬上播放
                    }
                }
            };

            var mBg = MakeMenu("背景(&B)");

            var mSolid = new ToolStripMenuItem("選擇單色...");
            mSolid.Click += (s, e) =>
            {
                StopGradient();
                using (var cd = new ColorDialog())
                    if (cd.ShowDialog() == DialogResult.OK)
                        ApplyBgColor(cd.Color);
            };

            var mGradient = new ToolStripMenuItem("漸變模式");
            mGradient.Click += (s, e) => StartGradient();

            var mResetBg = new ToolStripMenuItem("↩ 恢復預設");
            mResetBg.Click += (s, e) =>
            {
                StopGradient();
                pnlFilter.BackColor = Color.FromArgb(236, 240, 241);
                pnlSummary.BackColor = Color.White;
                pnlButtons.BackColor = Color.FromArgb(250, 250, 250);
                statusStrip.BackColor = Color.FromArgb(236, 240, 241);
            };

            mBg.DropDownItems.AddRange(new ToolStripItem[]
                { mSolid, mGradient, new ToolStripSeparator(), mResetBg });

            mMusic.DropDownItems.AddRange(new ToolStripItem[] { mSelect, new ToolStripSeparator(), mToggle });

            menuStrip.Items.AddRange(new ToolStripItem[] { mFile, mView, mBg, mMusic, mHelp });
        }

        private ToolStripMenuItem MakeMenu(string text)
            => new ToolStripMenuItem(text) { ForeColor = Color.White };

        // ── 篩選列 ────────────────────────────────────────────────
        private void BuildFilterPanel()
        {
            pnlFilter = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 48,
                BackColor = Color.FromArgb(236, 240, 241)
            };

            var lblY = Lbl("年份：", 10, 15);
            cmbYear  = new ComboBox { Left = 55, Top = 11, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblM = Lbl("月份：", 150, 15);
            cmbMonth = new ComboBox { Left = 196, Top = 11, Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMonth.Items.Add("全部");
            for (int m = 1; m <= 12; m++) cmbMonth.Items.Add($"{m} 月");
            cmbMonth.SelectedIndex = 0;

            var btnF   = MakeBtn("篩選",  288, Color.FromArgb(41, 128, 185));
            var btnAll = MakeBtn("全部",  365, Color.FromArgb(127, 140, 141));
            btnF  .Click += (s, e) => Refresh_();
            btnAll.Click += (s, e) => { cmbMonth.SelectedIndex = 0; txtSearch.Text = ""; Refresh_(); };

            var lblSearch = Lbl("搜尋：", 445, 15);
            txtSearch = new TextBox { Left = 510, Top = 11, Width = 180 };
            txtSearch.TextChanged += (s, e) => Refresh_();

            pnlFilter.Controls.AddRange(new Control[] { lblY, cmbYear, lblM, cmbMonth, btnF, btnAll, lblSearch, txtSearch });
        }

        // ── 摘要列（收入 / 支出 / 結餘）─────────────────────────
        private void BuildSummaryPanel()
        {
            pnlSummary = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Color.White
            };

            lblIncome  = SumLbl("總收入：$0",  Color.FromArgb(39, 174, 96),  20);
            lblExpense = SumLbl("總支出：$0",  Color.FromArgb(192, 57,  43), 300);
            lblBalance = SumLbl("結餘：$0",    Color.FromArgb(41, 128, 185), 580);

            pnlSummary.Controls.AddRange(new Control[] { lblIncome, lblExpense, lblBalance });
        }

        // ── 右側按鈕面板 ─────────────────────────────────────────
        private void BuildButtonPanel()
        {
            pnlButtons = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 110,
                BackColor = Color.FromArgb(250, 250, 250)
            };

            // (文字, 顏色, 點擊事件)
            var specs = new (string Text, Color Bg, EventHandler Handler)[]
            {
                ("➕ 新增", Color.FromArgb(39, 174, 96),  OnAdd),
                ("✏️ 編輯", Color.FromArgb(52, 152, 219), OnEdit),
                ("🗑 刪除", Color.FromArgb(192, 57,  43), OnDelete),
                ("📊 圖表", Color.FromArgb(155, 89, 182), OnChart)
            };

            for (int i = 0; i < specs.Length; i++)
            {
                var btn = new Button
                {
                    Text      = specs[i].Text,
                    Width     = 88, Height = 38,
                    Left      = 11,
                    Top       = 16 + i * 52,
                    BackColor = specs[i].Bg,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor    = Cursors.Hand,
                    Font      = new Font("微軟正黑體", 9.5f)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += specs[i].Handler;
                pnlButtons.Controls.Add(btn);
            }
        }

        // ── DataGridView ─────────────────────────────────────────
        private void BuildDataGridView()
        {
            dgv = new DataGridView
            {
                Dock                        = DockStyle.Fill,
                ReadOnly                    = true,
                AllowUserToAddRows          = false,
                AllowUserToDeleteRows       = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect                 = false,
                RowHeadersVisible           = false,
                BackgroundColor             = Color.White,
                BorderStyle                 = BorderStyle.None,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                EnableHeadersVisualStyles   = false,
                GridColor                   = Color.FromArgb(220, 220, 220),
                RowTemplate                 = { Height = 28 }
            };

            // 標題樣式
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(52, 73, 94);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font       = new Font("微軟正黑體", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment  = DataGridViewContentAlignment.MiddleCenter;
            // 隔行底色
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 250);

            // 欄位定義
            AddCol("colId",   "ID",   1,   visible: false);
            AddCol("colDate", "日期", 100);
            AddCol("colCat",  "類別", 85);
            AddCol("colDesc", "描述", 220);
            AddCol("colAmt",  "金額", 90,  align: DataGridViewContentAlignment.MiddleRight);
            AddCol("colType", "收支", 60,  align: DataGridViewContentAlignment.MiddleCenter);

            dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OnEdit(s, e); };
        }

        private void AddCol(string name, string header, int fw, bool visible = true,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft)
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name             = name,
                HeaderText       = header,
                FillWeight       = fw,
                Visible          = visible,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = align }
            });
        }

        // ── StatusStrip ──────────────────────────────────────────
        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip { BackColor = Color.FromArgb(236, 240, 241) };
            tsslInfo    = new ToolStripStatusLabel("就緒") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusStrip.Items.Add(tsslInfo);
        }

        // ── NotifyIcon（系統匣圖示）──────────────────────────────
        private void BuildNotifyIcon()
        {
            var trayMenu = new ContextMenuStrip();
            var tsOpen   = new ToolStripMenuItem("開啟記帳系統");
            tsOpen.Click += (s, e) => { Show(); WindowState = FormWindowState.Normal; BringToFront(); };
            var tsExit   = new ToolStripMenuItem("結束");
            tsExit.Click += (s, e) => System.Windows.Forms.Application.Exit();
            trayMenu.Items.AddRange(new ToolStripItem[] { tsOpen, tsExit });

            notifyIcon = new NotifyIcon
            {
                Text             = "個人記帳系統",
                Icon             = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible          = true
            };
            notifyIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
                BringToFront();
            };
        }

        // ══════════════════════════════════════════════════════════
        // 資料相關
        // ══════════════════════════════════════════════════════════
        private void FillYearCombo()
        {
            cmbYear.Items.Clear();
            cmbYear.Items.Add("全部");
            int cur = DateTime.Now.Year;
            for (int y = cur - 2; y <= cur + 1; y++) cmbYear.Items.Add(y.ToString());
            cmbYear.SelectedItem = cur.ToString();
            if (cmbYear.SelectedIndex < 0) cmbYear.SelectedIndex = 0;
        }

        private void Refresh_()
        {
            // 解析年份 / 月份篩選條件
            int? year = null, month = null;
            if (cmbYear.SelectedIndex > 0
                && int.TryParse(cmbYear.SelectedItem?.ToString(), out int y))
                year = y;
            if (cmbMonth.SelectedIndex > 0)
                month = cmbMonth.SelectedIndex; // index 1 = 1月, index 12 = 12月

            _current = _manager.GetFiltered(year, month);
            string kw = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(kw))
                _current = _current
                    .Where(r => r.Description.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0
                             || r.Category.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            LoadGrid();
            UpdateSummary();
        }

        private void LoadGrid()
        {
            dgv.Rows.Clear();
            foreach (RecordItem r in _current)
            {
                int idx = dgv.Rows.Add(
                    r.Id,
                    r.Date.ToString("yyyy/MM/dd"),
                    r.Category,
                    r.Description,
                    $"${r.Amount:N0}",
                    r.Type
                );
                var row = dgv.Rows[idx];
                // 收入 → 綠色；支出 → 紅色
                    var typeColor = 
                        r.Type == "收入"
                            ? Color.FromArgb(34, 153, 84)
                            : Color.FromArgb(192, 57, 43);

                row.Cells["colAmt"].Style.ForeColor = typeColor;
                row.Cells["colType"].Style.ForeColor = typeColor;
                if (CategoryColors.TryGetValue(r.Category, out Color bg))
                    row.DefaultCellStyle.BackColor = bg;    
            }
            tsslInfo.Text = $"共 {_current.Count} 筆紀錄";
        }

        private void UpdateSummary()
        {
            double inc = _current.Where(r => r.Type == "收入").Sum(r => r.Amount);
            double exp = _current.Where(r => r.Type == "支出").Sum(r => r.Amount);
            double bal = inc - exp;

            lblIncome .Text = $"總收入：${inc:N0}";
            lblExpense.Text = $"總支出：${exp:N0}";
            lblBalance.Text = $"結餘：${bal:N0}";
            lblBalance.ForeColor = bal >= 0
                ? Color.FromArgb(41, 128, 185)
                : Color.FromArgb(192, 57, 43);
        }

        /// <summary>取得目前 DataGridView 選取的紀錄</summary>
        private RecordItem GetSelected()
        {
            if (dgv.SelectedRows.Count == 0) return null;
            int id = (int)dgv.SelectedRows[0].Cells["colId"].Value;
            return _manager.Records.FirstOrDefault(r => r.Id == id);
        }

        // ══════════════════════════════════════════════════════════
        // 按鈕 / 選單事件
        // ══════════════════════════════════════════════════════════
        private void OnAdd(object s, EventArgs e)
        {
            AudioHelper.PlayClick();            // ← 加這行
            using (var frm = new frmRecord(null))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    _manager.Add(frm.Result);
                    Refresh_();
                    AudioHelper.PlaySuccess();  // ← 加這行

                }
            }
        }

        private void OnEdit(object s, EventArgs e)
        {
            AudioHelper.PlayClick();            // ← 加這行

            RecordItem item = GetSelected();
            if (item == null) { MessageBox.Show("請先選取一筆紀錄。", "提示"); return; }
            using (var frm = new frmRecord(item))
            {
                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    _manager.Update(frm.Result);
                    AudioHelper.PlaySuccess();  // ← 加這行

                    Refresh_();
                }
            }
        }

        private void OnDelete(object s, EventArgs e)
        {
            AudioHelper.PlayClick();            // ← 加這行

            RecordItem item = GetSelected();
            if (item == null) { MessageBox.Show("請先選取一筆紀錄。", "提示"); return; }
            if (MessageBox.Show($"確定刪除「{item.Description}」？",
                    "確認刪除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                _manager.Delete(item.Id);
                Refresh_();
                AudioHelper.PlayDelete();       // ← 加這行

            }
        }

        private void OnChart(object s, EventArgs e)
        {
            AudioHelper.PlayClick();            // ← 加這行

            if (_current.Count == 0)
            { MessageBox.Show("目前沒有資料可顯示圖表。", "提示"); return; }
            using (var frm = new frmChart(_current)) frm.ShowDialog(this);
        }

        private void OnExport(object s, EventArgs e)
        {
            AudioHelper.PlayClick();            // ← 加這行

            if (_current.Count == 0) { MessageBox.Show("目前沒有資料可匯出。", "提示"); return; }
            using (var sfd = new SaveFileDialog
            {
                Filter   = "CSV 檔案 (*.csv)|*.csv",
                FileName = $"記帳_{DateTime.Now:yyyyMM}.csv"
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _manager.ExportCsv(sfd.FileName, _current);
                    MessageBox.Show($"匯出成功！\n{sfd.FileName}",
                        "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        // ══════════════════════════════════════════════════════════
        // NotifyIcon 定時提醒
        // ══════════════════════════════════════════════════════════
        private void StartReminder()
        {
            // 啟動時若今天還沒記帳，立即提醒
            CheckToday();

            // 每分鐘檢查一次，晚上 21:00 提醒
            timerReminder = new System.Windows.Forms.Timer { Interval = 60_000 };
            timerReminder.Tick += (s, e) =>
            {
                if (DateTime.Now.Hour == 0&& DateTime.Now.Minute==52 && _lastRemindDate.Date != DateTime.Today)
                    CheckToday();
            };
            timerReminder.Start();
        }

        private void CheckToday()
        {
            _lastRemindDate = DateTime.Now;
            if (!_manager.Records.Any(r => r.Date.Date == DateTime.Today))
            {
                notifyIcon.ShowBalloonTip(4000,
                    "💰 記帳提醒",
                    $"今天（{DateTime.Today:M/d}）還沒有記帳，記得補上！",
                    ToolTipIcon.Info);
            }
        }

        // ══════════════════════════════════════════════════════════
        // Helper：建立常用控制項
        // ══════════════════════════════════════════════════════════
        private static Label Lbl(string text, int left, int top) =>
            new Label
            {
                Text      = text,
                AutoSize  = true,
                Left      = left,
                Top       = top,
                ForeColor = Color.FromArgb(44, 62, 80)
            };

        private static Button MakeBtn(string text, int left, Color bg) =>
            new Button
            {
                Text      = text,
                Width     = 68, Height = 28,
                Left      = left, Top = 10,
                BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };

        private static Label SumLbl(string text, Color color, int left) =>
            new Label
            {
                Text      = text,
                ForeColor = color,
                Font      = new Font("微軟正黑體", 13f, FontStyle.Bold),
                AutoSize  = true,
                Left      = left, Top = 12
            };

        private void OnImport(object s, EventArgs e)
        {
            using (var ofd = new OpenFileDialog
            {
                Title = "選擇要匯入的 CSV 檔案",
                Filter = "CSV 檔案 (*.csv)|*.csv|所有檔案|*.*"
            })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;

                var (success, fail) = _manager.ImportCsv(ofd.FileName);

                string msg = $"匯入完成！\n✅ 成功：{success} 筆";
                if (fail > 0) msg += $"\n⚠️ 略過：{fail} 筆（格式錯誤）";

                MessageBox.Show(msg, "匯入結果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Refresh_();
            }
        }
    }
}

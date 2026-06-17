using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.DataVisualization;
/*
 * ⚠ 需要在專案加入以下參考：
 *   Visual Studio → 專案 → 加入參考 → 組件 → System.Windows.Forms.DataVisualization
 */

namespace AccountingApp
{
    /// <summary>圖表視窗（圓餅圖 + 月份長條圖）</summary>
    public class frmChart : Form
    {
        private readonly List<RecordItem> _data;

        public frmChart(List<RecordItem> data)
        {
            _data = data;
            BuildUI();
        }

        // ══════════════════════════════════════════════════════════
        // 建立介面
        // ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text            = "📊 收支圖表分析";
            this.Size            = new Size(740, 520);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox     = false;
            this.Font            = new Font("微軟正黑體", 9.5f);
            this.BackColor       = Color.White;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildPieTab());
            tabs.TabPages.Add(BuildBarTab());
            this.Controls.Add(tabs);
        }

        // ══════════════════════════════════════════════════════════
        // Tab 1：圓餅圖（支出類別佔比）
        // ══════════════════════════════════════════════════════════
        private TabPage BuildPieTab()
        {
            var page = new TabPage("🥧 支出類別佔比");

            // 計算各類別支出
            var groups = _data
                .Where(r => r.Type == "支出")
                .GroupBy(r => r.Category)
                .Select(g => new { Cat = g.Key, Total = g.Sum(r => r.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            if (groups.Count == 0)
            {
                page.Controls.Add(EmptyLabel("此期間沒有支出資料"));
                return page;
            }

            var chart = MakeChart();
            double total = groups.Sum(x => x.Total);

            chart.Titles.Add(new Title($"支出類別分析  總計：${total:N0}")
            {
                Font = new Font("微軟正黑體", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            });

            var area = new ChartArea("pie");
            chart.ChartAreas.Add(area);

            var legend = new Legend("L")
            {
                Docking    = Docking.Bottom,
                Alignment  = StringAlignment.Center,
                Font       = new Font("微軟正黑體", 9f)
            };
            chart.Legends.Add(legend);

            var series = new Series("支出")
            {
                ChartType           = SeriesChartType.Pie,
                Legend              = "L",
                IsValueShownAsLabel = true,
                // 改後：加上 #AXISLABEL 顯示類別名稱
                Label = "#AXISLABEL\n$#VALY{N0}\n(#PERCENT{P0})"
            };
            // 標籤字型
            series["PieLabelStyle"] = "Outside";
            chart.Series.Add(series);

            foreach (var item in groups)
                series.Points.AddXY(item.Cat, item.Total);

            page.Controls.Add(chart);
            return page;
        }

        // ══════════════════════════════════════════════════════════
        // Tab 2：長條圖（月份收支對比）
        // ══════════════════════════════════════════════════════════
        private TabPage BuildBarTab()
        {
            var page = new TabPage("📊 月份收支對比");

            // 取得資料中涵蓋的所有 (年, 月)
            var months = _data
                .Select(r => new { r.Date.Year, r.Date.Month })
                .Distinct()
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            if (months.Count == 0)
            {
                page.Controls.Add(EmptyLabel("此期間沒有資料"));
                return page;
            }

            var chart = MakeChart();
            chart.Titles.Add(new Title("月份收支對比")
            {
                Font = new Font("微軟正黑體", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            });

            var area = new ChartArea("bar");
            area.AxisX.Title          = "月份";
            area.AxisY.Title          = "金額（元）";
            area.AxisX.Interval       = 1;
            area.AxisX.LabelStyle.Font = new Font("微軟正黑體", 8f);
            area.AxisY.LabelStyle.Format = "#,0";
            area.AxisY.LabelStyle.Font   = new Font("微軟正黑體", 8f);
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(220, 220, 220);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(220, 220, 220);
            chart.ChartAreas.Add(area);

            chart.Legends.Add(new Legend("L")
            {
                Docking = Docking.Bottom,
                Font    = new Font("微軟正黑體", 9f)
            });

            // 收入系列
            var sInc = new Series("收入")
            {
                ChartType = SeriesChartType.Column,
                Color     = Color.FromArgb(39, 174, 96),
                Legend    = "L",
                IsValueShownAsLabel = false
            };
            // 支出系列
            var sExp = new Series("支出")
            {
                ChartType = SeriesChartType.Column,
                Color     = Color.FromArgb(192, 57, 43),
                Legend    = "L",
                IsValueShownAsLabel = false
            };

            foreach (var m in months)
            {
                string lbl = $"{m.Year}/{m.Month:00}";
                double inc = _data.Where(r => r.Date.Year == m.Year && r.Date.Month == m.Month && r.Type == "收入").Sum(r => r.Amount);
                double exp = _data.Where(r => r.Date.Year == m.Year && r.Date.Month == m.Month && r.Type == "支出").Sum(r => r.Amount);
                sInc.Points.AddXY(lbl, inc);
                sExp.Points.AddXY(lbl, exp);
            }

            chart.Series.Add(sInc);
            chart.Series.Add(sExp);
            page.Controls.Add(chart);
            return page;
        }

        // ══════════════════════════════════════════════════════════
        // Helper
        // ══════════════════════════════════════════════════════════
        private static Chart MakeChart() =>
            new Chart
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };

        private static Label EmptyLabel(string text) =>
            new Label
            {
                Text      = text,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font      = new Font("微軟正黑體", 13f),
                ForeColor = Color.Gray
            };

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // frmChart
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "frmChart";
            this.Load += new System.EventHandler(this.frmChart_Load);
            this.ResumeLayout(false);

        }

        private void frmChart_Load(object sender, EventArgs e)
        {

        }
    }
}

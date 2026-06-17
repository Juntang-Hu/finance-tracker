using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AccountingApp
{
    /// <summary>負責記帳資料的讀取、寫入、CRUD 與 CSV 匯出</summary>
    public class RecordManager
    {
        // 資料存放的檔案（與執行檔同目錄）
        private const string DataFile = "records.dat";

        private List<RecordItem> _records = new List<RecordItem>();
        private int _nextId = 1;

        /// <summary>所有紀錄（唯讀）</summary>
        public IReadOnlyList<RecordItem> Records => _records.AsReadOnly();

        // ────────────────────────────────────────────────────────
        // 讀取檔案
        // ────────────────────────────────────────────────────────
        public void Load()
        {
            _records.Clear();
            if (!File.Exists(DataFile)) return;

            string[] lines = File.ReadAllLines(DataFile, Encoding.UTF8);
            // 第一行是標題列，跳過
            foreach (string line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try   { _records.Add(RecordItem.FromLine(line)); }
                catch { /* 格式錯誤的行直接略過 */ }
            }
            _nextId = _records.Count > 0 ? _records.Max(r => r.Id) + 1 : 1;
        }

        // ────────────────────────────────────────────────────────
        // 寫入檔案（每次異動後自動呼叫）
        // ────────────────────────────────────────────────────────
        private void Save()
        {
            var lines = new List<string> { "ID|Date|Category|Description|Amount|Type" };
            lines.AddRange(_records.OrderBy(r => r.Date).Select(r => r.ToLine()));
            File.WriteAllLines(DataFile, lines, Encoding.UTF8);
        }

        /// <summary>匯入 CSV，回傳 (成功筆數, 失敗筆數)</summary>
        public (int success, int fail) ImportCsv(string path)
        {
            if (!File.Exists(path)) return (0, 0);

            int success = 0, fail = 0;
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            foreach (string line in lines.Skip(1))  // 跳過標題列
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    // 最多切 5 段，讓「描述」欄位本身含有逗號也不會壞掉
                    string[] p = line.Split(new char[] { ',' }, 5);
                    if (p.Length < 5) { fail++; continue; }

                    string type = p[4].Trim();
                    if (type != "收入" && type != "支出") { fail++; continue; }

                    Add(new RecordItem
                    {
                        Date = DateTime.Parse(p[0].Trim()),
                        Category = p[1].Trim(),
                        Description = p[2].Trim(),
                        Amount = double.Parse(p[3].Trim()),
                        Type = type
                    });
                    success++;
                }
                catch { fail++; }
            }
            return (success, fail);
        }

        // ────────────────────────────────────────────────────────
        // CRUD
        // ────────────────────────────────────────────────────────
        public void Add(RecordItem item)
        {
            item.Id = _nextId++;
            _records.Add(item);
            Save();
        }

        public void Update(RecordItem item)
        {
            int i = _records.FindIndex(r => r.Id == item.Id);
            if (i >= 0) { _records[i] = item; Save(); }
        }

        public void Delete(int id)
        {
            _records.RemoveAll(r => r.Id == id);
            Save();
        }

        // ────────────────────────────────────────────────────────
        // 篩選查詢
        // ────────────────────────────────────────────────────────
        public List<RecordItem> GetFiltered(int? year, int? month)
        {
            IEnumerable<RecordItem> q = _records;
            if (year.HasValue)  q = q.Where(r => r.Date.Year  == year.Value);
            if (month.HasValue) q = q.Where(r => r.Date.Month == month.Value);
            return q.OrderByDescending(r => r.Date)
                    .ThenByDescending(r => r.Id)
                    .ToList();
        }

        // ────────────────────────────────────────────────────────
        // 匯出 CSV（給 Excel 使用）
        // ────────────────────────────────────────────────────────
        public void ExportCsv(string path, IEnumerable<RecordItem> items)
        {
            var lines = new List<string> { "日期,類別,描述,金額,收支" };
            lines.AddRange(items.Select(r =>
                $"{r.Date:yyyy/MM/dd},{r.Category},{r.Description},{r.Amount:F0},{r.Type}"));
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }
    }
}

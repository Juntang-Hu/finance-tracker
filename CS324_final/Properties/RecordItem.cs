using System;

namespace AccountingApp
{
    /// <summary>單筆記帳紀錄</summary>
    public class RecordItem
    {
        public int      Id          { get; set; }
        public DateTime Date        { get; set; }
        public string   Category    { get; set; }
        public string   Description { get; set; }
        public double   Amount      { get; set; }
        public string   Type        { get; set; }   // "收入" | "支出"

        // 序列化成一行（用 | 分隔，避免描述含逗號造成解析錯誤）
        public string ToLine() =>
            $"{Id}|{Date:yyyy-MM-dd}|{Category}|{Description}|{Amount:F2}|{Type}";

        // 從一行還原物件
        public static RecordItem FromLine(string line)
        {
            string[] p = line.Split('|');
            if (p.Length < 6) throw new FormatException("格式錯誤");
            return new RecordItem
            {
                Id          = int.Parse(p[0]),
                Date        = DateTime.Parse(p[1]),
                Category    = p[2],
                Description = p[3],
                Amount      = double.Parse(p[4]),
                Type        = p[5].Trim()
            };
        }
    }

    /// <summary>收入 / 支出類別清單</summary>
    public static class Categories
    {
        public static readonly string[] Income =
            { "薪資", "獎金", "投資", "兼職", "禮金", "其他收入" };

        public static readonly string[] Expense =
            { "餐飲", "交通", "住宿", "娛樂", "購物", "醫療", "教育", "通訊", "日用品", "其他支出" };
    }
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WMPLib;
using System.Windows.Forms;

namespace AccountingApp
{
    public static class AudioHelper
    {
        // kernel32 Beep（跟你的 BeepPlayer 一樣）
        [DllImport("kernel32.dll")]
        private static extern bool Beep(int frequency, int duration);

        private static WindowsMediaPlayer _bgm;
        private static bool _bgmOn = true;

        private static string _tempBgmPath;


        // ── 背景音樂 ──────────────────────────────────────────────
        public static void StartBgm(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                _bgm = new WindowsMediaPlayer();
                _bgm.URL = filePath;
                _bgm.settings.setMode("loop", true);
                _bgm.settings.volume = 25;   // 音量 0~100
                _bgm.controls.play();
            }
            catch { }
        }


        public static void ToggleBgm()
        {
            _bgmOn = !_bgmOn;
            try
            {
                if (_bgmOn) _bgm?.controls.play();
                else _bgm?.controls.pause();
            }
            catch { }
        }

        public static bool IsBgmOn => _bgmOn;

        // ── 點擊 / 操作音效（用 Task 避免卡住 UI）────────────────
        public static void PlayClick()
            => Task.Run(() => Beep(800, 30));       // 短促點擊聲

        public static void PlaySuccess()
            => Task.Run(() => { Beep(880, 70); Beep(1100, 90); });  // 上升音：儲存成功

        public static void PlayDelete()
            => Task.Run(() => { Beep(500, 60); Beep(300, 90); });   // 下降音：刪除

        // 設定檔存在 AppData（跟 exe 無關，不會被刪）
        private static string SettingsFile =>
            System.IO.Path.Combine(Application.UserAppDataPath, "bgm_path.txt");

        public static string LoadBgmPath()
        {
            if (!File.Exists(SettingsFile)) return "";
            return File.ReadAllText(SettingsFile).Trim();
        }

        public static void SaveBgmPath(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFile));
            File.WriteAllText(SettingsFile, path);
        }


        // 從內嵌資源播放（取代原本的 StartBgm）
        public static void StartBgmFromResource()
        {
            try
            {
                byte[] data = CS324_final.Properties.Resources.結算配樂_Moai_lalala_;
                _tempBgmPath = Path.Combine(Path.GetTempPath(), "accountingapp_bgm.mp3");
                File.WriteAllBytes(_tempBgmPath, data);
                StartBgm(_tempBgmPath);
            }
            catch { }
        }

        // StopBgm 加上清理暫存檔
        public static void StopBgm()
        {
            try { _bgm?.controls.stop(); } catch { }
            try
            {
                if (_tempBgmPath != null && File.Exists(_tempBgmPath))
                    File.Delete(_tempBgmPath);
            }
            catch { }
        }
    }
}
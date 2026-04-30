using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RegionCapture
{
    public class CaptureSettings
    {
        public string Folder;
        public string Prefix;
        public string Extension;    // "png","jpeg","tiff","bmp"
        public int StartIndex;
        public int Digits;
        public bool AddTimestamp; // 既存: 壁時計を右下に焼きこむ
        public bool BurnTimecode; // NEW : 経過タイムコードを左上に焼きこむ
        public int JpegQuality;  // 1-100
    }

    public class ScreenCapturer
    {
        // ─── タイマー精度 API ────────────────────────────────────────
        [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint u);
        [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint u);

        // ─── ディスク残量チェック ─────────────────────────────────────
        public static bool HasFreeDiskSpace(string folder, long requiredBytes)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(folder));
                foreach (var di in DriveInfo.GetDrives())
                    if (string.Equals(di.Name, root, StringComparison.OrdinalIgnoreCase))
                        return di.AvailableFreeSpace > requiredBytes;
            }
            catch { }
            return true;
        }

        // ─── キャプチャメインループ ──────────────────────────────────
        public async Task StartAsync(
            Rectangle rect,
            double fps,
            CaptureSettings settings,
            CancellationToken ct,
            Action<int, string> progress = null)
        {
            Directory.CreateDirectory(settings.Folder);

            var sw = new Stopwatch();
            double intervalMs = 1000.0 / Math.Max(1.0, fps);
            int index = settings.StartIndex;
            var startTime = DateTime.Now;   // タイムコード基準時刻

            // ★ タイマー精度を 1ms に設定（デフォルト ~15ms → 累積ズレ解消）
            timeBeginPeriod(1);
            try
            {
                sw.Start();
                while (!ct.IsCancellationRequested)
                {
                    string path = NextPath(settings, index);

                    using (var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(bmp))
                        {
                            g.CopyFromScreen(rect.Location, Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);

                            // 既存: 壁時計タイムスタンプ（右下）
                            if (settings.AddTimestamp)
                            {
                                using (var bgBr = new SolidBrush(Color.FromArgb(200, Color.Black)))
                                using (var fgBr = new SolidBrush(Color.White))
                                using (var f = new Font("Segoe UI", 12, FontStyle.Bold))
                                {
                                    string t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                    var sz = g.MeasureString(t, f);
                                    var p = new PointF(bmp.Width - sz.Width - 8, bmp.Height - sz.Height - 6);
                                    g.FillRectangle(bgBr, p.X - 4, p.Y - 2, sz.Width + 8, sz.Height + 4);
                                    g.DrawString(t, f, fgBr, p);
                                }
                            }

                            // ★ NEW: 経過タイムコード焼きこみ（左上・小さく）
                            // 形式: HH:MM:SS:FF  #0042
                            if (settings.BurnTimecode)
                            {
                                var elapsed = DateTime.Now - startTime;
                                int ff = (int)(elapsed.TotalSeconds * fps) % (int)Math.Max(1.0, fps);
                                string num = index.ToString().PadLeft(settings.Digits, '0');
                                string tc = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}:{ff:00}  #{num}";

                                using (var font = new Font("Consolas", 9, FontStyle.Regular))
                                using (var bgBr = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                                using (var fgBr = new SolidBrush(Color.FromArgb(255, 0, 230, 100)))
                                {
                                    var sz = g.MeasureString(tc, font);
                                    g.FillRectangle(bgBr, 4, 4, sz.Width + 8, sz.Height + 4);
                                    g.DrawString(tc, font, fgBr, 8, 6);
                                }
                            }
                        }
                        SaveBitmap(bmp, path, settings);
                    }

                    progress?.Invoke(index, path);
                    index++;

                    // 次フレームまでの正確な待機
                    double elapsed2 = sw.Elapsed.TotalMilliseconds;
                    double next = Math.Ceiling(elapsed2 / intervalMs) * intervalMs;
                    int delay = (int)Math.Max(0, next - elapsed2);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                timeEndPeriod(1); // ★ 必ず元に戻す
            }

            ct.ThrowIfCancellationRequested();
        }

        // ─── ファイルパス生成 ─────────────────────────────────────────
        private static string NextPath(CaptureSettings s, int index)
        {
            // ファイル名は常に Prefix + 連番 のみ。タイムスタンプはファイル名に付けない。
            string num = index.ToString(new string('0', s.Digits));
            string name = $"{s.Prefix}{num}.{s.Extension}";
            return Path.Combine(s.Folder, name);
        }

        // ─── 画像保存 ──────────────────────────────────────────────────
        private static void SaveBitmap(Bitmap bmp, string path, CaptureSettings s)
        {
            switch (s.Extension.ToLower())
            {
                case "png":
                    bmp.Save(path, ImageFormat.Png);
                    break;

                case "jpeg":
                case "jpg":
                    var enc = GetEncoder(ImageFormat.Jpeg);
                    var eps = new EncoderParameters(1);
                    eps.Param[0] = new EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality,
                        (long)Math.Max(1, Math.Min(100, s.JpegQuality)));
                    bmp.Save(path, enc, eps);
                    break;

                case "tiff":
                    bmp.Save(path, ImageFormat.Tiff);
                    break;

                case "bmp":
                    bmp.Save(path, ImageFormat.Bmp);
                    break;

                default:
                    bmp.Save(path, ImageFormat.Png);
                    break;
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var c in codecs)
                if (c.FormatID == format.Guid) return c;
            return null;
        }
    }
}
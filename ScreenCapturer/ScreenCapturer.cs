using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RegionCapture
{
    public class CaptureSettings
    {
        public string Folder;
        public string Prefix;
        public string Extension; // "png","jpeg","tiff","bmp"
        public int StartIndex;
        public int Digits;
        public bool AddTimestamp;
        public int JpegQuality; // 1-100
    }

    public class ScreenCapturer
    {
        public static bool HasFreeDiskSpace(string folder, long requiredBytes)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(folder));
                foreach (var di in DriveInfo.GetDrives())
                {
                    if (string.Equals(di.Name, root, StringComparison.OrdinalIgnoreCase))
                        return di.AvailableFreeSpace > requiredBytes;
                }
            }
            catch { }
            return true;
        }

        public async Task StartAsync(System.Drawing.Rectangle rect, double fps, CaptureSettings settings, CancellationToken ct, Action<int, string> progress = null)
        {
            Directory.CreateDirectory(settings.Folder);
            var sw = new Stopwatch();
            double frameIntervalMs = 1000.0 / Math.Max(1.0, fps);
            int index = settings.StartIndex;

            sw.Start();
            while (!ct.IsCancellationRequested)
            {
                string path = NextPath(settings, index);
                using (var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(rect.Location, System.Drawing.Point.Empty, rect.Size, CopyPixelOperation.SourceCopy);
                        if (settings.AddTimestamp)
                        {
                            using (var br = new SolidBrush(Color.FromArgb(200, Color.Black)))
                            using (var br2 = new SolidBrush(Color.White))
                            using (var f = new Font("Segoe UI", 12, FontStyle.Bold))
                            {
                                string t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                var sz = g.MeasureString(t, f);
                                var p = new System.Drawing.PointF(bmp.Width - sz.Width - 8, bmp.Height - sz.Height - 6);
                                g.FillRectangle(br, p.X - 4, p.Y - 2, sz.Width + 8, sz.Height + 4);
                                g.DrawString(t, f, br2, p);
                            }
                        }
                    }
                    SaveBitmap(bmp, path, settings);
                }

                progress?.Invoke(index, path);
                index++;

                double elapsed = sw.Elapsed.TotalMilliseconds;
                double next = Math.Ceiling(elapsed / frameIntervalMs) * frameIntervalMs;
                int delay = (int)Math.Max(0, next - elapsed);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }

            ct.ThrowIfCancellationRequested();
        }

        private static string NextPath(CaptureSettings s, int index)
        {
            string num = index.ToString(new string('0', s.Digits));
            string ts = s.AddTimestamp ? "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") : "";
            string name = $"{s.Prefix}{num}{ts}.{s.Extension}";
            return Path.Combine(s.Folder, name);
        }

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
                    eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)Math.Max(1, Math.Min(100, s.JpegQuality)));
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
            foreach (var c in codecs) if (c.FormatID == format.Guid) return c;
            return null;
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace RegionCapture
{
    public class FlashOverlay : Form
    {
        private readonly Rectangle _rect;
        private readonly Timer _timer;

        public FlashOverlay(Rectangle rect, int milliseconds = 1000)
        {
            _rect = rect;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;

            // 透明背景 + 枠のみ描画
            BackColor = Color.Fuchsia;
            TransparencyKey = Color.Fuchsia;
            DoubleBuffered = true;

            _timer = new Timer { Interval = Math.Max(50, milliseconds) };
            _timer.Tick += (s, e) => { _timer.Stop(); Close(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _timer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _timer?.Dispose(); } catch { }
            base.OnFormClosed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var pen = new Pen(Color.Lime, 3))
            using (var pen2 = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawRectangle(pen, _rect);
                e.Graphics.DrawRectangle(pen2, new Rectangle(_rect.X - 1, _rect.Y - 1, _rect.Width + 2, _rect.Height + 2));
            }
        }
    }
}

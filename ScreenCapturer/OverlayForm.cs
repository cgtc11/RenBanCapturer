using System;
using System.Drawing;
using System.Windows.Forms;

namespace RegionCapture
{
    public class OverlayForm : Form
    {
        private bool _drag;
        private Point _start;
        private Rectangle _rect;
        private readonly Pen _pen = new Pen(Color.Lime, 2);
        private readonly Brush _shade = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
        private readonly Brush _labelBg = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        private readonly Brush _labelFg = new SolidBrush(Color.White);
        private Bitmap _snapshot;

        public Rectangle SelectedRect { get; private set; }

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;

            BackColor = Color.Black;
            Opacity = 1.0;

            var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
            Bounds = new Rectangle(vs.Left, vs.Top, vs.Width, vs.Height);
            Cursor = Cursors.Cross;
            KeyPreview = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                var vs = System.Windows.Forms.SystemInformation.VirtualScreen;
                _snapshot = new Bitmap(vs.Width, vs.Height);
                using (var g = Graphics.FromImage(_snapshot))
                    g.CopyFromScreen(new Point(vs.Left, vs.Top), Point.Empty, new Size(vs.Width, vs.Height));
            }
            catch { }
            Invalidate();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try { _snapshot?.Dispose(); } catch { }
            base.OnFormClosed(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { DialogResult = DialogResult.Cancel; Close(); return; }
            if (e.Button != MouseButtons.Left) return;
            _drag = true;
            _start = e.Location;
            _rect = new Rectangle(e.Location, Size.Empty);
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_drag) return;
            _rect = MakeRect(_start, e.Location);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _drag = false;
            _rect = MakeRect(_start, e.Location);
            if (_rect.Width > 2 && _rect.Height > 2)
            {
                SelectedRect = _rect;
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (_snapshot != null) g.DrawImageUnscaled(_snapshot, 0, 0);

            using (var rgn = new Region(ClientRectangle))
            {
                if (_rect.Width > 0 && _rect.Height > 0) rgn.Exclude(_rect);
                g.FillRegion(_shade, rgn);
            }

            if (_rect.Width > 0 && _rect.Height > 0)
            {
                g.DrawRectangle(_pen, _rect);
                string info = $"X={_rect.X}, Y={_rect.Y}, W={_rect.Width}, H={_rect.Height}";
                using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
                {
                    var sz = g.MeasureString(info, f);
                    var px = _rect.Right - sz.Width - 6;
                    var py = _rect.Bottom + 6;
                    g.FillRectangle(_labelBg, px - 4, py - 2, sz.Width + 8, sz.Height + 4);
                    g.DrawString(info, f, _labelFg, px, py);
                }
            }
        }

        private static Rectangle MakeRect(Point a, Point b)
        {
            int x = Math.Min(a.X, b.X);
            int y = Math.Min(a.Y, b.Y);
            int w = Math.Abs(a.X - b.X);
            int h = Math.Abs(a.Y - b.Y);
            return new Rectangle(x, y, w, h);
        }
    }
}

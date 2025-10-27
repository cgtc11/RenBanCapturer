using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RegionCapture
{
    public partial class MainForm : Form
    {
        private readonly ScreenCapturer _capturer = new ScreenCapturer();
        private readonly Hotkey _hotkeySelect;
        private readonly Hotkey _hotkeyStartStop;

        private Rectangle _captureRect = Rectangle.Empty;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _autoCts;

        public MainForm()
        {
            InitializeComponent();

            // 既定値
            cmbFormat.SelectedIndex = 0;   // PNG
            numFps.Value = 24;             // ★24FPSに変更
            numDigits.Value = 4;
            numStart.Value = 1;
            txtPrefix.Text = "capture_";
            txtFolder.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "RegionCaps");

            // 自動停止 既定ON 30.0秒 最小0.1秒
            chkAutoStop.Checked = true;
            numAutoMin.Value = 0;
            numAutoSec.DecimalPlaces = 1;
            numAutoSec.Increment = 0.1M;
            numAutoSec.Minimum = 0.1M;
            numAutoSec.Maximum = 59.9M;
            numAutoSec.Value = 30.0M;

            // ホットキー
            _hotkeySelect = new Hotkey(this, ModKeys.None, Keys.F8);
            _hotkeyStartStop = new Hotkey(this, ModKeys.None, Keys.F9);
            _hotkeySelect.Pressed += (s, e) => SelectRegion();
            _hotkeyStartStop.Pressed += async (s, e) =>
            {
                if (_cts == null) await StartCaptureAsync();
                else await StopCaptureAsync();
            };

            // 起動時バックアップ
            try
            {
                Directory.CreateDirectory(txtFolder.Text);
                BackupExistingFiles(txtFolder.Text);
            }
            catch { }

            UpdateUiState();
        }

        // UIイベント
        private void btnSelectRegion_Click(object sender, EventArgs e) => SelectRegion();
        private async void btnStart_Click(object sender, EventArgs e) => await StartCaptureAsync();
        private async void btnStop_Click(object sender, EventArgs e) => await StopCaptureAsync();

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var f = new FolderBrowserDialog())
            {
                f.SelectedPath = Directory.Exists(txtFolder.Text)
                    ? txtFolder.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                if (f.ShowDialog(this) == DialogResult.OK) txtFolder.Text = f.SelectedPath;
            }
        }

        private void cmbFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isJpeg = string.Equals((string)cmbFormat.SelectedItem, "JPEG", StringComparison.OrdinalIgnoreCase);
            numJpegQ.Enabled = isJpeg;
            lblJpegQ.Enabled = isJpeg;
        }

        private void chkAutoStop_CheckedChanged(object sender, EventArgs e)
        {
            bool en = chkAutoStop.Checked;
            numAutoMin.Enabled = en;
            numAutoSec.Enabled = en;
        }

        private void btnApplyRegion_Click(object sender, EventArgs e)
        {
            if (!TryApplyRegionFromInputs()) return;
            UpdateRegionLabel();
            UpdateUiState();
            FlashRegion(_captureRect); // 1秒ハイライト
        }

        // 範囲選択
        private void SelectRegion()
        {
            // 範囲指定時バックアップ
            try
            {
                if (!string.IsNullOrWhiteSpace(txtFolder.Text))
                {
                    Directory.CreateDirectory(txtFolder.Text);
                    BackupExistingFiles(txtFolder.Text);
                }
            }
            catch { }

            using (var ov = new OverlayForm())
            {
                if (ov.ShowDialog(this) == DialogResult.OK)
                {
                    _captureRect = MakeEvenRect(ov.SelectedRect);
                    UpdateRegionInputsFromRect();
                    UpdateRegionLabel();
                }
            }
            UpdateUiState();
        }

        // 録画開始
        private async Task StartCaptureAsync()
        {
            if (_cts != null) return;

            // 数値入力→範囲
            TryApplyRegionFromInputs();

            if (_captureRect.Width < 2 || _captureRect.Height < 2)
            {
                MessageBox.Show(this, "キャプチャ範囲を指定してください（F8 または 数値入力→[範囲適用]）。",
                    "範囲未指定", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtFolder.Text))
            {
                MessageBox.Show(this, "出力フォルダを指定してください。",
                    "出力未指定", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Directory.CreateDirectory(txtFolder.Text);

            // 録画前バックアップ
            try { BackupExistingFiles(txtFolder.Text); } catch { }

            var fmtSel = (string)cmbFormat.SelectedItem;
            var settings = new CaptureSettings
            {
                Folder = txtFolder.Text,
                Prefix = txtPrefix.Text,
                Extension = fmtSel.ToLower(),   // png/jpeg/tiff/bmp
                StartIndex = (int)numStart.Value,
                Digits = (int)numDigits.Value,
                AddTimestamp = chkTimestamp.Checked,
                JpegQuality = (int)numJpegQ.Value
            };

            if (!ScreenCapturer.HasFreeDiskSpace(settings.Folder, 10L * 1024 * 1024))
            {
                MessageBox.Show(this, "出力先の空き容量が不足しています。",
                    "ディスク不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double fps = (double)numFps.Value;
            if (fps <= 0) fps = 1;

            _cts = new CancellationTokenSource();
            SetupAutoStopTimer();
            UpdateUiState();

            try
            {
                lblStatus.Text = "状態: 取得中";
                await _capturer.StartAsync(_captureRect, fps, settings, _cts.Token, progress: (i, path) =>
                {
                    if (IsHandleCreated)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            lblStatus.Text = $"状態: 取得中  #{i}  {Path.GetFileName(path)}";
                        }));
                    }
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                await StopCaptureAsync();
            }
        }

        // 自動停止タイマー
        private void SetupAutoStopTimer()
        {
            try { _autoCts?.Cancel(); } catch { }
            try { _autoCts?.Dispose(); } catch { }
            _autoCts = null;

            if (!chkAutoStop.Checked) return;

            double totalSec = (double)numAutoMin.Value * 60.0 + (double)numAutoSec.Value;
            if (totalSec < 0.1) totalSec = 0.1;

            _autoCts = new CancellationTokenSource();
            var token = _autoCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(totalSec), token);
                    if (token.IsCancellationRequested) return;
                    if (_cts != null) BeginInvoke((Action)(async () => { await StopCaptureAsync(); }));
                }
                catch { }
            }, token);
        }

        // 録画停止
        private async Task StopCaptureAsync()
        {
            var auto = Interlocked.Exchange(ref _autoCts, null);
            try { auto?.Cancel(); } catch { }
            try { auto?.Dispose(); } catch { }

            var cts = Interlocked.Exchange(ref _cts, null);
            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
                try { await Task.Delay(50); } catch { }
                try { cts.Dispose(); } catch { }
            }

            lblStatus.Text = "状態: 待機";
            UpdateUiState();
        }

        // 範囲の一時ハイライト
        private void FlashRegion(Rectangle r)
        {
            var overlay = new FlashOverlay(r, 1000);
            overlay.Show();
        }

        // 数値入力→範囲
        private bool TryApplyRegionFromInputs()
        {
            try
            {
                int x = (int)numX.Value;
                int y = (int)numY.Value;
                int w = (int)numW.Value;
                int h = (int)numH.Value;

                if (w < 2) w = 2;
                if (h < 2) h = 2;

                var vs = SystemInformation.VirtualScreen;
                if (x < vs.Left) x = vs.Left;
                if (y < vs.Top) y = vs.Top;
                if (x + w > vs.Right) w = Math.Max(2, vs.Right - x);
                if (y + h > vs.Bottom) h = Math.Max(2, vs.Bottom - y);

                _captureRect = MakeEvenRect(new Rectangle(x, y, w, h));
                UpdateRegionInputsFromRect();
                return true;
            }
            catch { return false; }
        }

        // 範囲→数値入力
        private void UpdateRegionInputsFromRect()
        {
            numX.Value = Math.Max(numX.Minimum, Math.Min(numX.Maximum, _captureRect.X));
            numY.Value = Math.Max(numY.Minimum, Math.Min(numY.Maximum, _captureRect.Y));
            numW.Value = Math.Max(numW.Minimum, Math.Min(numW.Maximum, _captureRect.Width));
            numH.Value = Math.Max(numH.Minimum, Math.Min(numH.Maximum, _captureRect.Height));
        }

        private void UpdateRegionLabel()
        {
            lblRegion.Text = (_captureRect.Width > 0 && _captureRect.Height > 0)
                ? $"領域: X={_captureRect.X}, Y={_captureRect.Y}, W={_captureRect.Width}, H={_captureRect.Height}"
                : "未選択";
        }

        private void UpdateUiState()
        {
            bool running = _cts != null;

            btnStart.Enabled = !running;
            btnStop.Enabled = running;
            btnSelectRegion.Enabled = !running;
            btnBrowse.Enabled = !running;

            cmbFormat.Enabled = !running;
            numFps.Enabled = !running;
            numDigits.Enabled = !running;
            numStart.Enabled = !running;
            chkTimestamp.Enabled = !running;
            numJpegQ.Enabled = !running && string.Equals((string)cmbFormat.SelectedItem, "JPEG", StringComparison.OrdinalIgnoreCase);

            chkAutoStop.Enabled = !running;
            numAutoMin.Enabled = !running && chkAutoStop.Checked;
            numAutoSec.Enabled = !running && chkAutoStop.Checked;

            btnApplyRegion.Enabled = !running;

            lblRegion.ForeColor = (_captureRect.Width > 0 && _captureRect.Height > 0)
                ? System.Drawing.Color.DarkGreen
                : System.Drawing.Color.DarkRed;
        }

        // 偶数幅・高さに丸める
        private static Rectangle MakeEvenRect(Rectangle r)
        {
            int w = (r.Width & ~1);
            int h = (r.Height & ~1);
            if (w < 2) w = 2;
            if (h < 2) h = 2;
            return new Rectangle(r.X, r.Y, w, h);
        }

        // バックアップ（ファイルのみ）
        private static void BackupExistingFiles(string folder)
        {
            if (!Directory.Exists(folder)) return;
            var files = Directory.GetFiles(folder);
            if (files.Length == 0) return;

            string backupDir = CreateNextBackupFolder(folder);
            foreach (var src in files)
            {
                try { File.Move(src, Path.Combine(backupDir, Path.GetFileName(src))); }
                catch { }
            }
        }

        private static string CreateNextBackupFolder(string parent)
        {
            for (int i = 1; i <= 999; i++)
            {
                string name = $"BackUP_{i:00}";
                string path = Path.Combine(parent, name);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    return path;
                }
            }
            throw new IOException("バックアップフォルダを作成できません。");
        }
    }
}

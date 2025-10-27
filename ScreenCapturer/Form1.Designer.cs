using System.Windows.Forms;

namespace RegionCapture
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox txtFolder;
        private Button btnBrowse;
        private TextBox txtPrefix;
        private ComboBox cmbFormat;
        private NumericUpDown numFps;
        private NumericUpDown numDigits;
        private NumericUpDown numStart;
        private CheckBox chkTimestamp;
        private Label lblStatus;
        private Label lblRegion;
        private Button btnSelectRegion;
        private Button btnStart;
        private Button btnStop;
        private NumericUpDown numJpegQ;
        private Label lblJpegQ;

        private CheckBox chkAutoStop;
        private NumericUpDown numAutoMin;
        private NumericUpDown numAutoSec;

        private NumericUpDown numX, numY, numW, numH;
        private Button btnApplyRegion;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.Text = "RenBanCapturer (.NET Framework 4.7)";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.AutoScaleMode = AutoScaleMode.None;

            this.ClientSize = new System.Drawing.Size(980, 340);

            int left = 14;
            int labelW = 130;
            int inputW = 600;
            int rowH = 28;
            int top = 14;
            int gap = 8;

            var lblFolder = new Label { Left = left, Top = top + 6, Width = labelW, Text = "出力フォルダ" };
            txtFolder = new TextBox { Left = left + labelW, Top = top, Width = inputW };
            btnBrowse = new Button { Left = left + labelW + inputW + 8, Top = top - 1, Width = 90, Text = "参照..." };
            btnBrowse.Click += btnBrowse_Click;
            top += rowH + gap;

            var lblPrefix = new Label { Left = left, Top = top + 6, Width = labelW, Text = "ファイル名プレフィックス" };
            txtPrefix = new TextBox { Left = left + labelW, Top = top, Width = 260 };

            var lblFormat = new Label { Left = left + labelW + 270, Top = top + 6, Width = 40, Text = "形式" };
            cmbFormat = new ComboBox { Left = left + labelW + 330, Top = top, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFormat.Items.AddRange(new object[] { "PNG", "JPEG", "TIFF", "BMP" });
            cmbFormat.SelectedIndexChanged += cmbFormat_SelectedIndexChanged;

            lblJpegQ = new Label { Left = left + labelW + 500, Top = top + 6, Width = 40, Text = "品質" };
            numJpegQ = new NumericUpDown { Left = left + labelW + 544, Top = top, Width = 60, Minimum = 1, Maximum = 100, Value = 90 };
            top += rowH + gap;

            var lblFps = new Label { Left = left, Top = top + 6, Width = labelW, Text = "FPS" };
            numFps = new NumericUpDown
            {
                Left = left + labelW,
                Top = top,
                Width = 120,
                Minimum = 1,
                Maximum = 120,
                DecimalPlaces = 0,
                Value = 24
            }; // ★24に変更

            var lblDigits = new Label { Left = left + 270, Top = top + 6, Width = 30, Text = "桁数" };
            numDigits = new NumericUpDown { Left = left + 310, Top = top, Width = 120, Minimum = 1, Maximum = 12, Value = 4 };

            var lblStart = new Label { Left = left + 450, Top = top + 6, Width = 70, Text = "開始番号" };
            numStart = new NumericUpDown { Left = left + 520, Top = top, Width = 140, Minimum = 0, Maximum = 999999999, Value = 1 };

            chkTimestamp = new CheckBox { Left = left + 680, Top = top + 4, Width = 200, Text = "ファイル名に時刻を付与" };
            top += rowH + gap;

            chkAutoStop = new CheckBox { Left = left, Top = top + 4, Width = 110, Text = "自動停止" };
            chkAutoStop.CheckedChanged += chkAutoStop_CheckedChanged;

            var lblAuto = new Label { Left = left + 130, Top = top + 6, Width = 50, Text = "停止まで" };
            numAutoMin = new NumericUpDown { Left = left + 190, Top = top, Width = 70, Minimum = 0, Maximum = 999, Value = 0 };
            var lblMin = new Label { Left = left + 262, Top = top + 6, Width = 24, Text = "分" };
            numAutoSec = new NumericUpDown { Left = left + 290, Top = top, Width = 80, Minimum = 0.1M, Maximum = 59.9M, DecimalPlaces = 1, Increment = 0.1M, Value = 30.0M };
            var lblSec = new Label { Left = left + 372, Top = top + 6, Width = 24, Text = "秒" };
            top += rowH + gap;

            var lblRegionCap = new Label { Left = left, Top = top + 6, Width = labelW, Text = "キャプチャ範囲" };
            lblRegion = new Label { Left = left + labelW, Top = top + 6, Width = 520, Text = "未選択" };
            btnSelectRegion = new Button { Left = left + labelW + 540, Top = top - 1, Width = 140, Text = "範囲選択 (F8)" };
            btnSelectRegion.Click += btnSelectRegion_Click;
            top += rowH + gap;

            var lblX = new Label { Left = left, Top = top + 6, Width = 20, Text = "X" };
            numX = new NumericUpDown { Left = left + 22, Top = top, Width = 90, Minimum = -10000, Maximum = 100000, DecimalPlaces = 0 };
            var lblY = new Label { Left = left + 120, Top = top + 6, Width = 20, Text = "Y" };
            numY = new NumericUpDown { Left = left + 142, Top = top, Width = 90, Minimum = -10000, Maximum = 100000, DecimalPlaces = 0 };
            var lblW = new Label { Left = left + 240, Top = top + 6, Width = 24, Text = "W" };
            numW = new NumericUpDown { Left = left + 266, Top = top, Width = 100, Minimum = 1, Maximum = 100000, DecimalPlaces = 0 };
            var lblH = new Label { Left = left + 372, Top = top + 6, Width = 24, Text = "H" };
            numH = new NumericUpDown { Left = left + 398, Top = top, Width = 100, Minimum = 1, Maximum = 100000, DecimalPlaces = 0 };
            btnApplyRegion = new Button { Left = left + 510, Top = top - 1, Width = 100, Text = "範囲適用" };
            btnApplyRegion.Click += btnApplyRegion_Click;
            top += rowH + gap;

            btnStart = new Button { Left = left + labelW, Top = top, Width = 180, Text = "開始 (F9)" };
            btnStop = new Button { Left = left + labelW + 200, Top = top, Width = 180, Text = "停止" };
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            top += rowH + gap;

            lblStatus = new Label { Left = left, Top = top + 6, Width = 940, Text = "状態: 待機" };

            Controls.AddRange(new Control[] {
                lblFolder, txtFolder, btnBrowse,
                lblPrefix, txtPrefix, lblFormat, cmbFormat, lblJpegQ, numJpegQ,
                lblFps, numFps, lblDigits, numDigits, lblStart, numStart, chkTimestamp,
                chkAutoStop, lblAuto, numAutoMin, lblMin, numAutoSec, lblSec,
                lblRegionCap, lblRegion, btnSelectRegion,
                lblX, numX, lblY, numY, lblW, numW, lblH, numH, btnApplyRegion,
                btnStart, btnStop,
                lblStatus
            });
        }
    }
}

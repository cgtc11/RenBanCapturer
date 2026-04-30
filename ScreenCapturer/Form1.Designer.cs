using System;
using System.Drawing;
using System.Windows.Forms;

namespace RegionCapture
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // ─── コントロール宣言 ──────────────────────────────────────────
        private TextBox txtFolder;
        private Button btnBrowse;
        private TextBox txtPrefix;
        private ComboBox cmbFormat;
        private NumericUpDown numFps;
        private NumericUpDown numDigits;
        private NumericUpDown numStart;
        private CheckBox chkTimestamp;
        private CheckBox chkBurnTimecode;   // ★ NEW
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
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // ══════════════════════════════════════════════════════════
            // フォーム基本設定
            // ══════════════════════════════════════════════════════════
            this.Text = "RenBanCapturer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.AutoScaleMode = AutoScaleMode.None;
            this.Font = new Font("Segoe UI", 9f);
            this.ClientSize = new Size(1020, 296);

            // ── ダークテーマ カラー定義 ──────────────────────────────
            var clrBg = Color.FromArgb(22, 24, 32);   // フォーム背景
            var clrCard = Color.FromArgb(32, 35, 46);   // カード背景
            var clrBorder = Color.FromArgb(52, 56, 72);   // カード枠
            var clrInput = Color.FromArgb(40, 43, 56);   // 入力欄背景
            var clrText = Color.FromArgb(220, 225, 235); // 通常テキスト
            var clrMuted = Color.FromArgb(130, 140, 160); // 薄いテキスト
            var clrAccent = Color.FromArgb(86, 182, 255);  // アクセント(青)
            var clrGreen = Color.FromArgb(39, 174, 96);   // 開始ボタン
            var clrRed = Color.FromArgb(192, 57, 43);   // 停止ボタン
            var clrBtn = Color.FromArgb(52, 110, 180);  // 一般ボタン

            this.BackColor = clrBg;

            // ══════════════════════════════════════════════════════════
            // ヘルパーメソッド
            // ══════════════════════════════════════════════════════════
            Label MkLabel(string text, int x, int y, int w = 110, bool muted = false)
            {
                return new Label
                {
                    Text = text,
                    Left = x,
                    Top = y,
                    Width = w,
                    AutoSize = false,
                    ForeColor = muted ? clrMuted : clrText,
                    BackColor = Color.Transparent,
                    TextAlign = ContentAlignment.MiddleLeft
                };
            }

            Label MkSectionLabel(string text, int x, int y)
            {
                return new Label
                {
                    Text = text,
                    Left = x,
                    Top = y,
                    AutoSize = true,
                    ForeColor = clrAccent,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
                };
            }

            TextBox MkTextBox(int x, int y, int w)
            {
                return new TextBox
                {
                    Left = x,
                    Top = y,
                    Width = w,
                    Height = 24,
                    BackColor = clrInput,
                    ForeColor = clrText,
                    BorderStyle = BorderStyle.FixedSingle
                };
            }

            NumericUpDown MkNum(int x, int y, int w,
                decimal min = 0, decimal max = 9999, decimal val = 0, int dec = 0)
            {
                return new NumericUpDown
                {
                    Left = x,
                    Top = y,
                    Width = w,
                    Height = 24,
                    Minimum = min,
                    Maximum = max,
                    Value = val,
                    DecimalPlaces = dec,
                    BackColor = clrInput,
                    ForeColor = clrText,
                    BorderStyle = BorderStyle.FixedSingle
                };
            }

            Button MkButton(string text, int x, int y, int w, int h, Color? backColor = null)
            {
                var b = new Button
                {
                    Text = text,
                    Left = x,
                    Top = y,
                    Width = w,
                    Height = h,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = backColor ?? clrBtn,
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 0;
                b.FlatAppearance.MouseOverBackColor =
                    Color.FromArgb(
                        Math.Min(255, (backColor ?? clrBtn).R + 25),
                        Math.Min(255, (backColor ?? clrBtn).G + 25),
                        Math.Min(255, (backColor ?? clrBtn).B + 25));
                return b;
            }

            CheckBox MkCheck(string text, int x, int y, int w)
            {
                // FlatStyle.Standard = OSシステム描画 → 白ボックス+濃いチェックマークで見やすい
                return new CheckBox
                {
                    Text = text,
                    Left = x,
                    Top = y,
                    Width = w,
                    Height = 22,
                    ForeColor = clrText,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Standard
                };
            }

            // カードパネル生成
            Panel MkCard(int x, int y, int w, int h)
            {
                var p = new Panel
                {
                    Left = x,
                    Top = y,
                    Width = w,
                    Height = h,
                    BackColor = clrCard,
                    BorderStyle = BorderStyle.FixedSingle
                };
                return p;
            }

            // ══════════════════════════════════════════════════════════
            // カード① ファイル設定 (x=10, y=10, w=1000, h=130)
            // ══════════════════════════════════════════════════════════
            const int cX = 10, cY = 10, cW = 1000;
            var card1 = MkCard(cX, cY, cW, 130);

            var lbSec1 = MkSectionLabel("▸  ファイル設定", 10, 8);

            // 行1: フォルダ (y=28)
            var lbFolder = MkLabel("出力フォルダ", 10, 32, 90);
            txtFolder = MkTextBox(105, 30, 726);
            btnBrowse = MkButton("参照…", 838, 28, 80, 26);
            btnBrowse.Click += btnBrowse_Click;

            // 行2: プレフィックス / 形式 (y=62)
            var lbPrefix = MkLabel("プレフィックス", 10, 66, 90);
            txtPrefix = MkTextBox(105, 64, 220);

            var lbFmt = MkLabel("形式", 334, 66, 30);
            cmbFormat = new ComboBox
            {
                Left = 368,
                Top = 64,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = clrInput,
                ForeColor = clrText,
                FlatStyle = FlatStyle.Flat
            };
            cmbFormat.Items.AddRange(new object[] { "PNG", "JPEG", "TIFF", "BMP" });
            cmbFormat.SelectedIndexChanged += cmbFormat_SelectedIndexChanged;

            lblJpegQ = MkLabel("品質", 498, 66, 30, muted: true);
            numJpegQ = MkNum(532, 64, 60, 1, 100, 90);

            // 行3: FPS / 桁数 / 開始番号 / チェックボックス (y=96)
            var lbFps = MkLabel("FPS", 10, 100, 30);
            numFps = MkNum(44, 98, 75, 1, 120, 24);

            var lbDig = MkLabel("桁数", 130, 100, 30);
            numDigits = MkNum(164, 98, 60, 1, 12, 4);

            var lbStart = MkLabel("開始番号", 234, 100, 60);
            numStart = MkNum(298, 98, 110, 0, 999999999, 1);

            chkTimestamp = MkCheck("壁時計（右下）", 420, 100, 130);
            chkBurnTimecode = MkCheck("TC焼込（左上）", 560, 100, 130); // ★ NEW

            card1.Controls.AddRange(new System.Windows.Forms.Control[] {
                lbSec1,
                lbFolder, txtFolder, btnBrowse,
                lbPrefix, txtPrefix,
                lbFmt, cmbFormat, lblJpegQ, numJpegQ,
                lbFps, numFps, lbDig, numDigits, lbStart, numStart,
                chkTimestamp, chkBurnTimecode
            });

            // ══════════════════════════════════════════════════════════
            // カード② 自動停止 (x=10, y=150, w=380, h=90)
            // ══════════════════════════════════════════════════════════
            var card2 = MkCard(10, 150, 380, 90);
            var lbSec2 = MkSectionLabel("▸  自動停止", 10, 8);

            chkAutoStop = MkCheck("自動停止を使用", 10, 32, 140);
            chkAutoStop.CheckedChanged += chkAutoStop_CheckedChanged;

            var lbAuto = MkLabel("停止まで", 158, 34, 56, muted: true);
            numAutoMin = MkNum(218, 32, 60, 0, 999, 0);
            var lbMin = MkLabel("分", 281, 34, 18, muted: true);
            numAutoSec = MkNum(302, 32, 66, 0.1M, 59.9M, 30.0M, 1);
            var lbSec2b = MkLabel("秒", 371, 34, 18, muted: true);

            card2.Controls.AddRange(new System.Windows.Forms.Control[] {
                lbSec2,
                chkAutoStop, lbAuto, numAutoMin, lbMin, numAutoSec, lbSec2b
            });

            // ══════════════════════════════════════════════════════════
            // カード③ キャプチャ範囲 (x=400, y=150, w=610, h=90)
            // ══════════════════════════════════════════════════════════
            var card3 = MkCard(400, 150, 610, 90);
            var lbSec3 = MkSectionLabel("▸  キャプチャ範囲", 10, 8);

            lblRegion = new Label
            {
                Left = 10,
                Top = 30,
                Width = 340,
                Height = 22,
                Text = "未選択",
                ForeColor = Color.FromArgb(220, 80, 80),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnSelectRegion = MkButton("範囲選択  F8", 360, 28, 130, 26);
            btnSelectRegion.Click += btnSelectRegion_Click;

            // X Y W H + 適用
            var lbX = MkLabel("X", 10, 63, 14, muted: true);
            numX = MkNum(26, 61, 82, -10000, 100000, 0);
            var lbY = MkLabel("Y", 116, 63, 14, muted: true);
            numY = MkNum(132, 61, 82, -10000, 100000, 0);
            var lbW = MkLabel("W", 222, 63, 16, muted: true);
            numW = MkNum(240, 61, 82, 1, 100000, 1);
            var lbH = MkLabel("H", 330, 63, 14, muted: true);
            numH = MkNum(346, 61, 82, 1, 100000, 1);
            btnApplyRegion = MkButton("範囲適用", 440, 59, 90, 26);
            btnApplyRegion.Click += btnApplyRegion_Click;

            card3.Controls.AddRange(new System.Windows.Forms.Control[] {
                lbSec3,
                lblRegion, btnSelectRegion,
                lbX, numX, lbY, numY, lbW, numW, lbH, numH, btnApplyRegion
            });

            // ══════════════════════════════════════════════════════════
            // ボトムバー: 開始 / 停止 / ステータス
            // ══════════════════════════════════════════════════════════
            const int botY = 252;

            btnStart = MkButton("▶  開始   F9", 10, botY, 155, 36, clrGreen);
            btnStop = MkButton("■  停止", 174, botY, 110, 36, clrRed);
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnStart.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStop.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

            lblStatus = new Label
            {
                Left = 298,
                Top = botY + 6,
                Width = 714,
                Height = 24,
                Text = "状態: 待機",
                ForeColor = clrMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };

            // ══════════════════════════════════════════════════════════
            // フォームに追加
            // ══════════════════════════════════════════════════════════
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                card1, card2, card3,
                btnStart, btnStop, lblStatus
            });

            this.ResumeLayout(false);
        }
    }
}
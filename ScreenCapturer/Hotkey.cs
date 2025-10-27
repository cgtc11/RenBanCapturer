using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RegionCapture
{
    [Flags]
    public enum ModKeys : uint
    {
        None = 0x0000,
        Alt = 0x0001,
        Ctrl = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000
    }

    public class Hotkey : IDisposable
    {
        private static int _idSeed = 0x1000;
        private readonly int _id;
        private readonly IntPtr _handle;
        private MessageWindow _msgWin;

        public event EventHandler Pressed;

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public Hotkey(Form form, ModKeys mods, Keys key)
        {
            _id = System.Threading.Interlocked.Increment(ref _idSeed);
            _handle = form.Handle;
            if (!RegisterHotKey(_handle, _id, (uint)mods, (uint)key))
                throw new InvalidOperationException("ホットキー登録に失敗しました。");

            form.HandleDestroyed += (s, e) => { try { UnregisterHotKey(_handle, _id); } catch { } };
            form.FormClosed += (s, e) => Dispose();

            form.Load += (s, e) =>
            {
                _msgWin = new MessageWindow(form, _id, () => Pressed?.Invoke(this, EventArgs.Empty));
            };
        }

        public void Dispose()
        {
            try { UnregisterHotKey(_handle, _id); } catch { }
        }

        private class MessageWindow : NativeWindow
        {
            private readonly int _id;
            private readonly Action _cb;

            public MessageWindow(Form host, int id, Action cb)
            {
                AssignHandle(host.Handle);
                _id = id;
                _cb = cb;
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_HOTKEY = 0x0312;
                if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == _id)
                {
                    _cb?.Invoke();
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}

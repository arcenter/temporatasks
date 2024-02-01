using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TemporaTasks.Core
{
    public static class KeyboardHooks
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        private const int HOTKEY_ID = 4329;

        //Modifiers:
        private const uint MOD_NONE = 0x0000;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        private const uint VK_KEY = 0xDE;

        private static HwndSource source;
        private static IntPtr handle;

        public static void Register()
        {
            handle = new WindowInteropHelper(mainWindow).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            if (!RegisterHotKey(handle, HOTKEY_ID, MOD_WIN, VK_KEY)) mainWindow.TrayIcon.ShowBalloonTip("Hotkey Registration Failed", "Hehe failed", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
        }

        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_KEY)
                            {
                                mainWindow.WindowHide(mainWindow.IsActive);
                            }
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public static void Unregister()
        {
            source.RemoveHook(HwndHook);
            UnregisterHotKey(handle, HOTKEY_ID);
        }
    }
}

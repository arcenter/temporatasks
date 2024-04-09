using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TemporaTasks.Windows;

namespace TemporaTasks.Core
{
    public static class KeyboardHooks
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        private const int HOTKEY_ID_1 = 4329;
        private const int HOTKEY_ID_2 = 4330;

        private const uint MOD_NONE = 0x0000, MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_WIN = 0x0008;

        private const uint VK_KEY_1 = 0xBF;
        private const uint VK_KEY_2 = 0xDE;

        private static HwndSource source;
        private static IntPtr handle;

        public static void Register()
        {
            handle = new WindowInteropHelper(mainWindow).Handle;
            source = HwndSource.FromHwnd(handle);
            source.AddHook(HwndHook);

            if (!RegisterHotKey(handle, HOTKEY_ID_1, MOD_WIN, VK_KEY_1)) mainWindow.OnTaskDue("Hotkey Registration Failed", "TemporaTasks wasn't able to register hotkeys, as another application might be using them", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
            if (!RegisterHotKey(handle, HOTKEY_ID_2, MOD_WIN, VK_KEY_2)) mainWindow.OnTaskDue("Hotkey Registration Failed", "TemporaTasks wasn't able to register hotkeys, as another application might be using them", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Error);
        }

        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    int vkey;
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID_1:
                            vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_KEY_1) mainWindow.WindowHide(mainWindow.IsActive);
                            handled = true;
                            break;
                        case HOTKEY_ID_2:
                            vkey = (((int)lParam >> 16) & 0xFFFF);
                            if (vkey == VK_KEY_2)
                            {
                                foreach (Window _window in Application.Current.Windows) if (_window.IsActive) goto end;
                                GlobalAddTask window = new();
                                window.Show();
                                window.Activate();
                            }
                            end:
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
            UnregisterHotKey(handle, HOTKEY_ID_1);
            UnregisterHotKey(handle, HOTKEY_ID_2);
        }
    }
}

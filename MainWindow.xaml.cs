using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

using TemporaTasks.Pages;
using TemporaTasks.Core;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using TemporaTasks.Windows;
using TemporaTasks.UserControls;

namespace TemporaTasks
{
    public partial class MainWindow : Window
    {
        public HomePage homePage;

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += Window1_SourceInitialized;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (TaskFile.saveFilePath == null)
            {
                string path = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TemporaTasks\\";
                TaskFile.backupPath = $"{path}backups\\";

                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                if (!Directory.Exists(TaskFile.backupPath)) Directory.CreateDirectory(TaskFile.backupPath);

                TaskFile.saveFilePath = $"{path}data.json";
                TaskFile.LoadData();
            }
            else
            {
                IsHitTestVisible = false;
                await Task.Delay(500);
                IsHitTestVisible = true;
            }
        }

        public void LoadPage()
        {
            homePage = new();
            FrameView.Navigate(homePage);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q && (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))) TaskFile.SaveData(this);
            else if (e.Key == Key.F && NoTextBoxHasFocus(this)) MaximizeButton_MouseDown(null, null);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                WindowHide();
                e.Handled = true;
            }
        }

        private bool NoTextBoxHasFocus(DependencyObject parent)
        {
            foreach (var textBox in GetVisualChildren<TextBox>(parent))
                if (textBox.IsFocused)
                    return false;
            return true;
        }

        private IEnumerable<T> GetVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    if (child is T typedChild)
                        yield return typedChild;
                    foreach (var grandChild in GetVisualChildren<T>(child))
                        yield return grandChild;
                }
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MinimizeButton.Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 1 : 0, TimeSpan.FromMilliseconds(250)));
            MinimizeX1.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 1 : 0.25, TimeSpan.FromMilliseconds(250)));
        }

        private void MinimizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MaximizeButton.Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 1 : 0, TimeSpan.FromMilliseconds(250)));
            MaximizeRect.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 1 : 0.25, TimeSpan.FromMilliseconds(250)));
        }

        private void MaximizeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                WindowBorder.BorderThickness = new Thickness(1);
            }
            else
            {
                WindowState = WindowState.Maximized;
                WindowBorder.BorderThickness = new Thickness(0);
            }
                
        }

        private void CloseButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CloseButton.Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue)? 1 : 0, TimeSpan.FromMilliseconds(250)));
            X1.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue)? 1 : 0.25, TimeSpan.FromMilliseconds(250)));
            X2.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue)? 1 : 0.25, TimeSpan.FromMilliseconds(250)));
        }

        private void CloseButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift))
            {
                TaskFile.SaveData(this);
                return;
            }
            WindowHide();
        }

        public delegate void WindowUnHidden();
        public event WindowUnHidden IsWindowUnHidden;

        public async void WindowHide(bool hide = true)
        {
            if (hide)
            {
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
                await Task.Delay(201);
                Hide();
                BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromTicks(0)));
            }
            else
            {
                IsWindowUnHidden?.Invoke();
                Show();
                Activate();
                Topmost = true;
                Topmost = false;
                await Task.Delay(25);
                homePage.Focus();
            }
        }

        bool balloonCalledRecently = false;
        public async void OnTaskDue(IndividualTask task)
        {
            if (balloonCalledRecently || TaskFile.notificationMode == TaskFile.NotificationMode.Muted) return;
            balloonCalledRecently = true;
            if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal || (TaskFile.notificationMode == TaskFile.NotificationMode.High && task.taskPriority == IndividualTask.TaskPriority.High))
                TrayIcon.ShowBalloonTip($"{((task.taskPriority == IndividualTask.TaskPriority.High) ? "⚠" : "")}Task Due!", task.IsGarbled() ? "Garbled Task" : task.TaskName, BalloonIcon.Info);
            await Task.Delay(2500);
            if (TaskFile.notifPopupMode && !IsActive) WindowHide(false);
            balloonCalledRecently = false;
        }

        public async void HotkeyRegFailNotif()
        {
            TrayIcon.ShowBalloonTip("Hotkey Registration Failed", "TemporaTasks wasn't able to register hotkeys, as another application might be using them", BalloonIcon.Error);
        }

        private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            WindowHide(IsActive);
        }

        private void TrayIcon_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            TaskFile.SaveData(this);
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void window_Deactivated(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            KeyboardHooks.Register();
        }

        protected override void OnClosed(EventArgs e)
        {
            KeyboardHooks.Unregister();
            base.OnClosed(e);
        }

        // -----------------------------------------------------------------------------------

        private const int WM_SYSCOMMAND = 0x112;
        private HwndSource hwndSource;

        private enum ResizeDirection
        {
            Left = 61441,
            Right = 61442,
            Top = 61443,
            TopLeft = 61444,
            TopRight = 61445,
            Bottom = 61446,
            BottomLeft = 61447,
            BottomRight = 61448,
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private void Window1_SourceInitialized(object sender, EventArgs e)
        {
            hwndSource = PresentationSource.FromVisual((Visual)sender) as HwndSource;
        }

        private void ResizeWindow(ResizeDirection direction)
        {
            SendMessage(hwndSource.Handle, WM_SYSCOMMAND, (IntPtr)direction, IntPtr.Zero);
        }

        protected void ResetCursor(object sender, MouseEventArgs e)
        {
            try
            {
                if (Mouse.LeftButton != MouseButtonState.Pressed)
                {
                    this.Cursor = Cursors.Arrow;
                    FrameView.IsHitTestVisible = true;
                    homePage.WindowIsResizing(false);
                }
            } catch { }
        }

        protected void Resize(object sender, MouseButtonEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;

            FrameView.IsHitTestVisible = false;
            homePage.WindowIsResizing(true);
            var clickedShape = sender as Shape;

            switch (clickedShape.Name)
            {
                case "ResizeN":
                    this.Cursor = Cursors.SizeNS;
                    ResizeWindow(ResizeDirection.Top);
                    break;
                case "ResizeE":
                    this.Cursor = Cursors.SizeWE;
                    ResizeWindow(ResizeDirection.Right);
                    break;
                case "ResizeS":
                    this.Cursor = Cursors.SizeNS;
                    ResizeWindow(ResizeDirection.Bottom);
                    break;
                case "ResizeW":
                    this.Cursor = Cursors.SizeWE;
                    ResizeWindow(ResizeDirection.Left);
                    break;
                case "ResizeNW":
                    this.Cursor = Cursors.SizeNWSE;
                    ResizeWindow(ResizeDirection.TopLeft);
                    break;
                case "ResizeNE":
                    this.Cursor = Cursors.SizeNESW;
                    ResizeWindow(ResizeDirection.TopRight);
                    break;
                case "ResizeSE":
                    this.Cursor = Cursors.SizeNWSE;
                    ResizeWindow(ResizeDirection.BottomRight);
                    break;
                case "ResizeSW":
                    this.Cursor = Cursors.SizeNESW;
                    ResizeWindow(ResizeDirection.BottomLeft);
                    break;
                default:
                    break;
            }
        }

        protected void DisplayResizeCursor(object sender, MouseEventArgs e)
        {
            if (WindowState == WindowState.Maximized) return;
            Cursor = (sender as Shape).Name switch
            {
                "ResizeN" or "ResizeS" => Cursors.SizeNS,
                "ResizeE" or "ResizeW" => Cursors.SizeWE,
                "ResizeNW" or "ResizeSE" => Cursors.SizeNWSE,
                "ResizeNE" or "ResizeSW" => Cursors.SizeNESW,
                _ => Cursors.Arrow
            };
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (FrameView.IsHitTestVisible) return;
            FrameView.IsHitTestVisible = true;
            ResetCursor(null, null);
        }

        // -----------------------------------------------------------------------------------
    }
}
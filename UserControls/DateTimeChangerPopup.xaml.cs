using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TemporaTasks.Core;

namespace TemporaTasks.UserControls
{
    public partial class DateTimeChangerPopup : UserControl
    {
        readonly Popup popupObject;
        readonly List<IndividualTask> tasks;

        public DateTimeChangerPopup(List<IndividualTask> tasks, Popup popupObject)
        {
            InitializeComponent();
            this.popupObject = popupObject;
            this.tasks = tasks;
        }

        public DateTimeChangerPopup(Popup popupObject)
        {
            InitializeComponent();
            this.popupObject = popupObject;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown += PopupMouseDown;
            mainWindow.KeyDown += PopupKeyDown;
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
            await Task.Delay(250);
            DateTextbox.Focus();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown -= PopupMouseDown;
            mainWindow.KeyDown -= PopupKeyDown;
        }

        private void PopupMouseDown(object sender, MouseEventArgs e)
        {
            if (IsMouseOver) return;
            PopupClose();
        }

        private void PopupKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                PopupClose();

            else if (e.Key == Key.Enter)
            {
                DateTime? newDueTime;

                DateBorder.BorderThickness = TimeBorder.BorderThickness = new Thickness(0);
                try
                {
                    newDueTime = DTHelper.StringToDateTime(DateTextbox.Text, TimeTextbox.Text);
                }
                catch (IncorrectDateException)
                {
                    DateBorder.BorderThickness = new Thickness(2);
                    return;
                }
                catch (IncorrectTimeException)
                {
                    TimeBorder.BorderThickness = new Thickness(2);
                    return;
                }

                if (tasks is null)
                {
                    TaskFile.NotificationModeTimer.Interval = newDueTime.Value - DateTime.Now;

                    TaskFile.notificationMode = TaskFile.NotificationMode.Muted;
                    ((MainWindow)Application.Current.MainWindow).homePage.UpdateNotificationMode();

                    TaskFile.NotificationModeTimerStart = newDueTime.Value;
                    TaskFile.NotificationModeTimer.Start();
                }
                else
                {
                    foreach (IndividualTask task in tasks)
                    {
                        task.TaskTimer.Stop();
                        task.DueDT = newDueTime;
                        task.NewDueDT();
                    }

                    TaskFile.SaveData();
                }
                PopupClose();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DTHelper.StringToDateTime(DateTextbox.Text, TimeTextbox.Text);
                DateBorder.BorderThickness = new Thickness(0);
                TimeBorder.BorderThickness = new Thickness(0);
            }
            catch (IncorrectDateException)
            {
                if (((TextBox)sender).Name == "DateTextbox") DateBorder.BorderThickness = new Thickness(2);
            }
            catch (IncorrectTimeException)
            {
                if (((TextBox)sender).Name == "TimeTextbox") TimeBorder.BorderThickness = new Thickness(2);
            }
        }

        public async void PopupClose(int delay = 200)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(delay)));
            await Task.Delay(delay+25);
            popupObject.IsOpen = false;
            popupObject.Child = null;
        }

        private void Textbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
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
        Popup popupObject;
        IndividualTask task;

        public DateTimeChangerPopup(IndividualTask task, Popup popupObject)
        {
            InitializeComponent();
            this.popupObject = popupObject;
            this.task = task;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown += PopupMouseDown;
            mainWindow.KeyDown += PopupKeyDown;
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
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

                DateBorder.BorderThickness = TimeBorder.BorderThickness = new Thickness(0);
                DateTime? newDueDate;
                try
                {
                    newDueDate = DTHelper.StringToDateTime(DateTextbox.Text, TimeTextbox.Text);
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

                task.TaskTimer.Stop();
                task.DueDT = newDueDate;
                task.NewDueDT();

                TaskFile.SaveData();

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

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0, TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void TimeChange_MouseDown(object sender, MouseButtonEventArgs e)
        {
            task.ChangeDueTime(sender, e);
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (((Border)sender).Name)
            {
                case "Edit":
                    PopupClose();
                    task.EditIcon_MouseDown(null, null);
                    return;

                case "CopyTT":
                    Clipboard.SetText(task.Name);
                    PopupClose();
                    return;

                case "WontDo":
                    task.WontDoTask();
                    return;

                case "Garble":
                    task.Garble(null, true);
                    return;

                case "ToggleHP":
                    task.ToggleHP();
                    return;

                case "OpenLink":
                    task.LinkOpen();
                    PopupClose();
                    return;

                case "Delete":
                    PopupClose();
                    task.TrashIcon_MouseDown(null, null);
                    return;

                default:
                    return;
            }
        }

        private void Textbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
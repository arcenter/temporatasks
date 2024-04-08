using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TemporaTasks.Core;
using TemporaTasks.Pages;
using TemporaTasks.UserControls;

namespace TemporaTasks.Windows
{
    public partial class GlobalAddTask : Window
    {
        private Window window;
        public DateTimePicker datePickerUserControl;

        public GlobalAddTask()
        {
            InitializeComponent();
            TaskNameTextbox.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2; ;
            BeginAnimation(Window.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !datePickerPopUp.IsOpen)
            {
                AddButton_MouseDown(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                if (datePickerPopUp.IsOpen) datePickerPopUp.IsOpen = false;
                else CloseWindow();
            }
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void Calendar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            datePickerUserControl = new(dateTextBox.Text)
            {
                textBox = dateTextBox,
                popUp = datePickerPopUp
            };
            datePickerPopUp.Child = datePickerUserControl;
            datePickerPopUp.IsOpen = true;
        }

        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseWindow();
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NameBorder.BorderThickness = DateBorder.BorderThickness = TimeBorder.BorderThickness = new Thickness(0);

            if (TaskNameTextbox.Text.Length == 0)
            {
                NameBorder.BorderThickness = new Thickness(2);
                return;
            }

            Nullable<DateTime> newDueDate;
            try
            {
                newDueDate = DTHelper.StringToDateTime(dateTextBox.Text, timeTextBox.Text);
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

            if (DTHelper.matchedDate != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedDate, "");
            if (DTHelper.matchedTime != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedTime, "");
            TaskNameTextbox.Text = TaskNameTextbox.Text.Trim();

            long randomLong = (long)(new Random().NextDouble() * long.MaxValue);
            TaskFile.TaskList.Add(new IndividualTask(randomLong, TaskNameTextbox.Text, DateTimeOffset.UtcNow.LocalDateTime, newDueDate, null, null, null, false, IndividualTask.TaskPriority.Normal));
            TaskFile.SaveData();
            ((MainWindow)Application.Current.MainWindow).FrameView.Navigate(new HomePage());
            CloseWindow();
        }

        private void Textbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void TaskNameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string? temp;

            temp = DTHelper.RegexRelativeTimeMatch(TaskNameTextbox.Text);
            if (temp != null) timeTextBox.Text = temp;

            temp = DTHelper.RegexRelativeDateMatch(TaskNameTextbox.Text);
            if (temp != null) dateTextBox.Text = temp;
        }

        private async void CloseWindow()
        {
            BeginAnimation(Window.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            await Task.Delay(250);
            Close();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (TaskNameTextbox.Text.Length != 0) return;
            try { Close(); }
            catch { }
        }
    }
}

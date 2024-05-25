using System;
using System.Collections;
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
            if (e.Key == Key.System)
            {
                if (e.SystemKey == Key.N)
                {
                    TaskNameTextbox.Focus();
                    e.Handled = true;
                    return;
                }

                else if (e.SystemKey == Key.D)
                {
                    dateTextBox.Focus();
                    e.Handled = true;
                    return;
                }

                else if (e.SystemKey == Key.T)
                {
                    timeTextBox.Focus();
                    e.Handled = true;
                    return;
                }

                else if (e.SystemKey == Key.G)
                {
                    TagsTextbox.Focus();
                    e.Handled = true;
                    return;
                }

                else if (e.SystemKey == Key.P)
                {
                    HighPriority_MouseDown(null, null);
                    e.Handled = true;
                    return;
                }

                else if (e.SystemKey == Key.B)
                {
                    GarbleTask_MouseDown(null, null);
                    e.Handled = true;
                    return;
                }

                else
                    foreach (Line L in new Line[] { L1, L2, L3, L4, L5, L6 })
                        L.Visibility = Visibility.Visible;
            }

            if (e.Key == Key.Enter && !datePickerPopUp.IsOpen)
            {
                if (TagsTextbox.IsFocused)
                {
                    if (TagsTextbox.Text.Length != 0)
                    {
                        if (TagsTextbox.Text.Trim() != "" && !TagsTextbox.Text.Contains(';'))
                        {
                            TagsStackAdd(TagsTextbox.Text);
                            TagsTextbox.Clear();
                            e.Handled = true;
                        }
                        return;
                    }
                }
                AddButton_MouseDown(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                if (datePickerPopUp.IsOpen) datePickerPopUp.IsOpen = false;
                else CloseWindow();
            }
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                foreach (Line L in new Line[] { L1, L2, L3, L4, L5, L6 })
                    L.Visibility = Visibility.Collapsed;
                e.Handled = true;
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
            tooltip.Placement = PlacementMode.Relative;
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

            //if (DTHelper.matchedDate != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedDate, "");
            //if (DTHelper.matchedTime != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedTime, "");
            TaskNameTextbox.Text = TaskNameTextbox.Text.Trim();

            ArrayList? tagList = [];
            foreach (Tags tag in TagsStack.Children)
                tagList.Add(tag.TagText);

            bool garbled = L6checkMark.Opacity == 1;

            IndividualTask.TaskPriority taskPriority = (L5checkMark.Opacity == 1) ? IndividualTask.TaskPriority.High : IndividualTask.TaskPriority.Normal;

            long randomLong = (long)(new Random().NextDouble() * long.MaxValue);
            TaskFile.TaskList.Add(new IndividualTask(randomLong, TaskNameTextbox.Text, "", DateTimeOffset.UtcNow.LocalDateTime, newDueDate, null, tagList, null, garbled, taskPriority));
            TaskFile.SaveData();
            ((MainWindow)Application.Current.MainWindow).FrameView.Navigate(new HomePage());
            CloseWindow();
        }

        private void Textbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                DTHelper.StringToDateTime(dateTextBox.Text, timeTextBox.Text);
                DateBorder.BorderThickness = new Thickness(0);
                TimeBorder.BorderThickness = new Thickness(0);
            }
            catch (IncorrectDateException)
            {
                if (((TextBox)sender).Name == "dateTextBox") DateBorder.BorderThickness = new Thickness(2);
            }
            catch (IncorrectTimeException)
            {
                if (((TextBox)sender).Name == "timeTextBox") TimeBorder.BorderThickness = new Thickness(2);
            }
        }

        //private void TaskNameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    string? temp;

        //    temp = DTHelper.RegexRelativeTimeMatch(TaskNameTextbox.Text);
        //    if (temp != null) timeTextBox.Text = temp;

        //    temp = DTHelper.RegexRelativeDateMatch(TaskNameTextbox.Text);
        //    if (temp != null) dateTextBox.Text = temp;
        //}

        private void TagsStackAdd(string value)
        {
            foreach (Tags tag in TagsStack.Children)
                if (tag.TagText == value) return;
            TagsStack.Children.Add(new Tags(value));
        }

        private void TagsTextbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && TagsTextbox.Text.Trim() != "" && !TagsTextbox.Text.Contains(';'))
            {
                TagsStackAdd(TagsTextbox.Text);
                TagsTextbox.Clear();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (TagsTextbox.Text.Length == 0 && TagsStack.Children.Count > 0) TagsStack.Children.RemoveAt(TagsStack.Children.Count - 1);
            }
        }

        private void HighPriority_MouseDown(object sender, MouseButtonEventArgs e)
        {
            L5checkMark.Opacity = (L5checkMark.Opacity + 1) % 2;
        }

        private void GarbleTask_MouseDown(object sender, MouseButtonEventArgs e)
        {
            L6checkMark.Opacity = (L6checkMark.Opacity + 1) % 2;
        }

        private async void CloseWindow()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
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
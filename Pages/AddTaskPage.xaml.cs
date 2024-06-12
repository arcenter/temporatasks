using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TemporaTasks.Core;
using TemporaTasks.UserControls;

namespace TemporaTasks.Pages
{
    public partial class AddTaskPage : Page
    {

        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public DateTimePicker datePickerUserControl;

        public AddTaskPage()
        {
            InitializeComponent();
            TaskNameTextbox.Focus();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown += Page_KeyDown;
            mainWindow.PreviewKeyUp += Page_PreviewKeyUp;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
            mainWindow.PreviewKeyUp -= Page_PreviewKeyUp;
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
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
                else mainWindow.FrameView.GoBack();
            }
        }

        private void Page_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                foreach (Line L in new Line[] { L1, L2, L3, L4, L5, L6 })
                    L.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        private void TaskStackScroller_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (System.Windows.Forms.SystemInformation.MouseWheelScrollLines == -1)
                    e.Handled = false;
                else
                {
                    ScrollViewer SenderScrollViewer = (ScrollViewer)sender;
                    SenderScrollViewer.ScrollToVerticalOffset(SenderScrollViewer.VerticalOffset - e.Delta);
                    e.Handled = true;
                }
                if (datePickerPopUp.IsOpen) datePickerPopUp.IsOpen = false;
            }
            catch { }
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void calendar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            datePickerUserControl = new DateTimePicker(dateTextBox.Text)
            {
                textBox = dateTextBox,
                popUp = datePickerPopUp
            };
            datePickerPopUp.Child = datePickerUserControl;
            datePickerPopUp.IsOpen = true;
        }

        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.FrameView.GoBack();
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

            Nullable<TimeSpan> recurranceTimeSpan;
            try
            {
                recurranceTimeSpan = DTHelper.RecurranceStringToDateTime(RecurranceTextBox.Text);
            }
            catch
            {
                RecurranceBorder.BorderThickness = new Thickness(2);
                return;
            }

            //if (DTHelper.matchedDate != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedDate, "");
            //if (DTHelper.matchedTime != null) TaskNameTextbox.Text = TaskNameTextbox.Text.Replace(DTHelper.matchedTime, "");
            TaskNameTextbox.Text = TaskNameTextbox.Text.Trim();

            long randomLong;
            randomGen:
            randomLong = (long)(new Random().NextDouble() * long.MaxValue);
            foreach (IndividualTask task in TaskFile.TaskList) if (task.TaskUID == randomLong) { goto randomGen; }

            // TODO
            // this can probably be optimized by keeping a list of UIDs

            ArrayList? tagList = [];
            foreach (Tags tag in TagsStack.Children)
                tagList.Add(tag.TagText);

            bool garbled = L6checkMark.Opacity == 1;

            IndividualTask.TaskPriority taskPriority = (L5checkMark.Opacity == 1) ? IndividualTask.TaskPriority.High : IndividualTask.TaskPriority.Normal;

            TaskFile.TaskList.Add(new IndividualTask(randomLong, TaskNameTextbox.Text, TaskDescTextbox.Text, DateTimeOffset.UtcNow.LocalDateTime, newDueDate, null, IndividualTask.TaskStatus.Normal, tagList, recurranceTimeSpan, garbled, taskPriority));
            TaskFile.SaveData();
            mainWindow.FrameView.GoBack();
        }

        //private void TaskNameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    string? temp;

        //    temp = DTHelper.RegexRelativeTimeMatch(TaskNameTextbox.Text);
        //    if (temp != null) timeTextBox.Text = temp;

        //    temp = DTHelper.RegexRelativeDateMatch(TaskNameTextbox.Text);
        //    if (temp != null) dateTextBox.Text = temp;
        //}

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
    }
}
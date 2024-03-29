﻿using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TemporaTasks.Core;
using TemporaTasks.UserControls;

namespace TemporaTasks.Pages
{
    public partial class EditTaskPage : Page
    {

        readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        readonly IndividualTask task;

        public DateTimePicker datePickerUserControl;

        public EditTaskPage(IndividualTask task)
        {
            InitializeComponent();

            this.task = task;
            TaskNameTextbox.Text = task.TaskName;
            
            if (task.DueDT.HasValue)
            {
                dateTextBox.Text = DTHelper.DateToString(task.DueDT.Value);
                timeTextBox.Text = DTHelper.TimeToString(task.DueDT.Value);
            }

            if (task.TagList != null)
                foreach (String tag in task.TagList)
                    TagsStackAdd(tag);

            TaskNameTextbox.Focus();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown += Page_KeyDown;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
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
                ConfirmButton_MouseDown(null, null);
            }

            else if (e.Key == Key.Escape)
            {
                if (datePickerPopUp.IsOpen) datePickerPopUp.IsOpen = false;
                else mainWindow.FrameView.GoBack();
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

        private void ConfirmButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DateBorder.BorderThickness = TimeBorder.BorderThickness = new Thickness(0);
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

            ArrayList? tagList = [];
            foreach (Tags tag in TagsStack.Children)
                tagList.Add(tag.TagText);

            task.TaskTimer.Stop();
            TaskFile.TaskList[TaskFile.TaskList.IndexOf(task)] = new IndividualTask(task.TaskUID, TaskNameTextbox.Text, task.CreatedDT, newDueDate, null, tagList, null, task.IsGarbled());
            TaskFile.SaveData();
            mainWindow.FrameView.RemoveBackEntry();
            mainWindow.FrameView.Navigate(new HomePage());
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

            // Error - '#something #thing' comes as a whole
            //Match match = TagIdentifier().Match(TaskNameTextbox.Text);
            //if (match.Success) TagsStackAdd(match.Value[2..^1]);
        }

        //[GeneratedRegex(" #.{1,} ")]
        //private static partial Regex TagIdentifier();

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
    }
}
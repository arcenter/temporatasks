﻿using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using TemporaTasks.Core;
using TemporaTasks.UserControls;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TemporaTasks.Pages
{
    public partial class HomePage : Page
    {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        Nullable<int> currentFocus = null;

        IndividualTask lastTask;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SortComboBox.SelectedIndex = TaskFile.sortType;
            mainWindow.KeyDown += Page_KeyDown;
            mainWindow.KeyUp += Page_KeyUp;
            mainWindow.MouseDown += Window_MouseDown;
            if (TaskFile.TaskList.Count == 0)
            {
                NewTaskArrow.Visibility = Visibility.Visible;
            }
            else
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    task.IsTrashIconClicked += TrashIcon_MouseDown;
                    task.IsEditIconClicked += EditIcon_MouseDown;
                    task.Cursor = Cursors.Hand;
                }

                GenerateTaskStack();
                if (currentFocus.HasValue) FocusTask();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
            mainWindow.KeyUp -= Page_KeyUp;
            mainWindow.MouseDown -= Window_MouseDown;
            foreach (IndividualTask task in TaskFile.TaskList)
            {
                task.StrokeOff(); // StrokeBorder.BorderThickness = new Thickness(0);
                task.IsTrashIconClicked -= TrashIcon_MouseDown;
                task.IsEditIconClicked -= EditIcon_MouseDown;
                task.Cursor = Cursors.None;
            }
            TaskStack.Children.Clear();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused)
            {
                if (e.Key == Key.Escape || e.Key == Key.Tab)
                {
                    label.Focus();
                    if (SearchTextBox.Text.Length == 0) SearchTextBoxAnimate();
                }
                return;
            }

            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.F))
            {
                SearchTextBox.Focus();
                SearchTextBoxAnimate(true);
                return;
            }

            switch (e.Key)
            {
                case Key.N:
                    AddButton_MouseDown(null, null);
                    break;

                case Key.S:
                case Key.OemQuestion:
                    SearchTextBoxAnimate(true);
                    break;

                case Key.Escape:
                    if (currentFocus.HasValue)
                    {
                        currentFocus = null;
                        UnfocusTasks();
                    }
                    else mainWindow.WindowHide();
                    break;
            }

            if (currentFocus.HasValue)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        PreviousTaskFocus();
                        break;

                    case Key.Down:
                        NextTaskFocus();
                        break;

                    case Key.Space:
                        ToggleTaskCompletion((IndividualTask)TaskStack.Children[currentFocus.Value]);
                        break;

                    case Key.E:
                    case Key.Enter:
                        mainWindow.FrameView.Navigate(new EditTaskPage((IndividualTask)TaskStack.Children[currentFocus.Value]));
                        break;

                    case Key.D:
                    case Key.Delete:
                        TrashIcon_MouseDown((IndividualTask)TaskStack.Children[currentFocus.Value]);
                        if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = TaskStack.Children.Count - 1;
                        FocusTask();
                        break;
                    
                    case Key.Z:
                        TaskFile.TaskList.Add(lastTask);
                        TaskFile.SaveData();
                        GenerateTaskStack();
                        break;
                }
            }
            else
                switch (e.Key)
                {
                    case Key.Up:
                        currentFocus = 0;
                        PreviousTaskFocus();
                        break;

                    case Key.Down:
                        currentFocus = 0;
                        FocusTask();
                        break;
                }
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused) return;

            switch (e.Key)
            {
                case Key.S:
                case Key.OemQuestion:
                    SearchTextBox.Focus();
                    break;
            }
        }

        private void PreviousTaskFocus()
        {
            do
            {
                currentFocus--;
                if (currentFocus.Value < 0) currentFocus = TaskStack.Children.Count - 1;
            } while (TaskStack.Children[currentFocus.Value] is not IndividualTask);
            FocusTask();
        }

        private void NextTaskFocus()
        {
            do
            {
                currentFocus++;
                if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = 0;
            } while (!(TaskStack.Children[currentFocus.Value] is IndividualTask));

            FocusTask();
        }

        private void ToggleTaskCompletion(IndividualTask sender)
        {
            sender.ToggleCompletionStatus();
            TaskFile.SaveData();

            int temp = TaskStack.Children.IndexOf((IndividualTask)sender);
            if (temp > -1) currentFocus = temp;
            FocusTask();
        }

        private void EditIcon_MouseDown(object sender)
        {
            mainWindow.FrameView.Navigate(new EditTaskPage((IndividualTask)sender));
        }

        private void TrashIcon_MouseDown(object sender)
        {
            IndividualTask task = (IndividualTask)sender;
            task.TaskTimer.Stop();
            lastTask = task;
            TaskFile.TaskList.Remove(task);
            TaskFile.SaveData();
            GenerateTaskStack();
        }

        private void AddButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskStack.Children.RemoveRange(0, TaskStack.Children.Count-1);
            mainWindow.FrameView.Navigate(new AddTaskPage());
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void FocusTask()
        {
            UnfocusTasks();
            int count = TaskStack.Children.Count;
            if (count > 0)
            {
                if (!(currentFocus.Value > 0 && currentFocus.Value < count)) currentFocus = 0;

                while (!(TaskStack.Children[currentFocus.Value] is IndividualTask)) currentFocus++;

                ((IndividualTask)TaskStack.Children[currentFocus.Value]).StrokeOn();
                ((IndividualTask)TaskStack.Children[currentFocus.Value]).BringIntoView();
            }
        }

        private void UnfocusTasks()
        {
            foreach (object obj in TaskStack.Children) if (obj is IndividualTask) ((IndividualTask)obj).StrokeOff();
        }
        
        private void GenerateTaskStack()
        {
            if (TaskStack == null) return;

            TaskStack.Children.Clear();

            Dictionary<IndividualTask, object> matchesSort = new(), completed = new(), sortedDict = new();
            ArrayList doesntMatchSort = new();

            string GetDaySuffix(int day)
            {
                switch (day)
                {
                    case 1:
                    case 21:
                    case 31:
                        return "st";
                    case 2:
                    case 22:
                        return "nd";
                    case 3:
                    case 23:
                        return "rd";
                    default:
                        return "th";
                }
            }

            ArrayList days = new();
            Regex regex = new(SearchTextBox.Text.ToLower());
            switch (SortComboBox.SelectedIndex)
            {
                case 1:
                case 2:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (regex.Match(task.TaskName.ToLower()).Success)
                            if (task.IsCompleted)
                                completed[task] = task.CompletedDT.Value;
                            else
                            if (task.DueDT.HasValue)
                                matchesSort[task] = (SortComboBox.SelectedIndex == 1) ? task.CreatedDT.Value : task.DueDT.Value;
                            else doesntMatchSort.Add(task);

                    sortedDict = matchesSort.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                    completed = completed.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

                    foreach (IndividualTask task in sortedDict.Keys)
                    {
                        DateTime date = (SortComboBox.SelectedIndex == 1) ? task.CreatedDT.Value : task.DueDT.Value;
                        if (!days.Contains(date.ToShortDateString()))
                        {
                            days.Add(date.ToShortDateString());
                            TaskStack.Children.Add(new Label()
                            {
                                Content = date.ToString("dddd, d") + GetDaySuffix(date.Day) + date.ToString(" MMMM yyyy"),
                                Foreground = (SolidColorBrush)mainWindow.FindResource("Border"),
                                FontFamily = new FontFamily(new Uri("pack://TemporaTasks:,,,/Resources/Fonts/Manrope.ttf"), "Manrope Light"),
                                FontSize = 14,
                                Margin = new Thickness(9, (TaskStack.Children.Count > 0)? 28 : 0, 0, 0)
                            });
                        }
                        TaskStack.Children.Add(task);
                    }

                    if (doesntMatchSort.Count > 0)
                    {
                        TaskStack.Children.Add(new Label()
                        {
                            Content = "No date",
                            Foreground = (SolidColorBrush)mainWindow.FindResource("Border"),
                            FontFamily = new FontFamily(new Uri("pack://TemporaTasks:,,,/Resources/Fonts/Manrope.ttf"), "Manrope Light"),
                            FontSize = 14,
                            Margin = new Thickness(9, (TaskStack.Children.Count > 0) ? 28 : 0, 0, 0)
                        });

                        foreach (IndividualTask task in doesntMatchSort) TaskStack.Children.Add(task);
                    }

                    if (completed.Count > 0)
                    {
                        TaskStack.Children.Add(new Label()
                        {
                            Content = "Completed",
                            Foreground = (SolidColorBrush)mainWindow.FindResource("Border"),
                            FontFamily = new FontFamily(new Uri("pack://TemporaTasks:,,,/Resources/Fonts/Manrope.ttf"), "Manrope Light"),
                            FontSize = 14,
                            Margin = new Thickness(9, (TaskStack.Children.Count > 0) ? 28 : 0, 0, 0)
                        });

                        foreach (IndividualTask task in completed.Keys) TaskStack.Children.Add(task);

                    }
                    
                    break;

                default:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (regex.Match(task.TaskName.ToLower()).Success)
                            if (task.IsCompleted)
                                completed[task] = task.CreatedDT.Value;
                            else
                                matchesSort[task] = task.TaskName;

                    sortedDict = matchesSort.OrderBy(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                    completed = completed.OrderBy(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);

                    foreach (IndividualTask task in sortedDict.Keys) TaskStack.Children.Add(task);
                    foreach (IndividualTask task in doesntMatchSort) TaskStack.Children.Add(task);
                    foreach (IndividualTask task in completed.Keys) TaskStack.Children.Add(task);
                    break;
            }
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TaskFile.sortType = SortComboBox.SelectedIndex;
            TaskFile.SaveData();
            GenerateTaskStack();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchTextBox.IsMouseDirectlyOver && !SearchBorder.IsMouseDirectlyOver)
            {
                label.Focus();
                if (SearchTextBox.Text.Length == 0) SearchTextBoxAnimate();
            }
            UnfocusTasks();
            currentFocus = 0;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateTaskStack();
        }

        private void SearchBorder_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!SearchTextBox.IsMouseOver && !SearchTextBox.IsFocused && SearchTextBox.Text.Length == 0)
            {
                SearchTextBoxAnimate((bool)e.NewValue);
            }
        }

        private void SearchBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SearchTextBox.Focus();
        }

        private async void SearchTextBoxAnimate(bool open = false)
        {
            {
                DoubleAnimation ani = new(open ? 190 : 0, TimeSpan.FromMilliseconds(500))
                {
                    EasingFunction = new QuarticEase()
                    {
                        EasingMode = EasingMode.EaseInOut
                    }
                };

                SearchTextBox.BeginAnimation(WidthProperty, ani);
            }

            if (open)
            {
                SearchIcon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                DoubleAnimation ani = new(1.25, TimeSpan.FromMilliseconds(250));
                SearchIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                SearchIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);

                await Task.Delay(251);

                DoubleAnimation ani2 = new(1, TimeSpan.FromMilliseconds(250));
                SearchIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani2);
                SearchIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani2);
            }
        }
    }
}
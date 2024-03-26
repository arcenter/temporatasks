using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TemporaTasks.Core;
using TemporaTasks.UserControls;

namespace TemporaTasks.Pages
{
    public partial class HomePage : Page
    {
        readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        public DispatcherTimer UpdateTaskTimersTimer = new() { Interval = TimeSpan.FromMinutes(5) };

        int? currentFocus = null;
        
        bool reverseSort = false;
        bool garbleMode = false;
        Dictionary<string, ArrayList> days = [];

        DateTime? dateClipboard = null;
        List<IndividualTask> lastTask = [];

        public HomePage()
        {
            InitializeComponent();
            UpdateTaskTimersTimer.Tick += UpdateTaskTimers;
            UpdateTaskTimersTimer.Start();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SortComboBox.SelectedIndex = TaskFile.sortType;
            mainWindow.KeyDown += Page_KeyDown;
            mainWindow.KeyUp += Page_KeyUp;
            mainWindow.MouseDown += Window_MouseDown;
            mainWindow.IsWindowUnHidden += Window_Unhidden;
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
                }

                GenerateTaskStack();
                if (currentFocus.HasValue) FocusTask();
            }
            TaskFile.NotificationsOn = !TaskFile.NotificationsOn;
            NotifButton_MouseDown(null, null);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
            mainWindow.KeyUp -= Page_KeyUp;
            mainWindow.MouseDown -= Window_MouseDown;
            mainWindow.IsWindowUnHidden -= Window_Unhidden;
            foreach (IndividualTask task in TaskFile.TaskList)
            {
                task.StrokeOff();
                task.Background_MouseLeave(null, null);
                task.IsTrashIconClicked -= TrashIcon_MouseDown;
                task.IsEditIconClicked -= EditIcon_MouseDown;
            }
            TaskStack.Children.Clear();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused)
            {
                if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape)
                {
                    HomePagePage.Focus();
                    if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
                    if (e.Key == Key.Enter || e.Key == Key.Tab)
                    {
                        currentFocus = 0;
                        FocusTask();
                    }
                }
                return;
            }

            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.F))
            {
                SearchTextBox.Focus();
                RunSearchTextBoxCloseAnimation(true);
                return;
            }

            else if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) && Keyboard.IsKeyDown(Key.G))
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                    task.Garble(null, TaskStackScroller);
                return;
            }

            switch (e.Key)
            {
                case Key.Home:
                    currentFocus = 0;
                    FocusTask();
                    return;

                case Key.End:
                    currentFocus = 0;
                    PreviousTaskFocus();
                    return;
                
                case Key.K:
                    SearchTextBox.Text = "$n";
                    RunSearchTextBoxCloseAnimation(true);
                    return;

                case Key.H:
                    currentFocus = null;
                    UnfocusTasks();
                    HomePagePage.Focus();
                    SearchTextBox.Text = "";
                    RunSearchTextBoxCloseAnimation();
                    return;

                case Key.S:
                case Key.OemQuestion:
                    currentFocus = null;
                    UnfocusTasks();
                    RunSearchTextBoxCloseAnimation(true);
                    return;

                case Key.R:
                    currentFocus = null;
                    UnfocusTasks();
                    ReverseButton_MouseDown(null, null);
                    return;

                case Key.N:
                    currentFocus = null;
                    AddButton_MouseDown(null, null);
                    return;

                case Key.M:
                    NotifButton_MouseDown(null, null);
                    return;

                case Key.Escape:
                    if (currentFocus.HasValue)
                    {
                        currentFocus = null;
                        UnfocusTasks();
                    }
                    else mainWindow.WindowHide();
                    return;
            }

            if (currentFocus.HasValue)
            {
                IndividualTask task = (IndividualTask)TaskStack.Children[currentFocus.Value];

                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                {
                    if (Keyboard.IsKeyDown(Key.Down))
                    {
                        int limit = TaskStack.Children.Count;
                        do
                        {
                            currentFocus++;
                            if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = 0;
                            if (--limit <= 0)
                            {
                                currentFocus = null;
                                return;
                            }
                        } while (TaskStack.Children[currentFocus.Value] is IndividualTask && limit > 0);
                        NextTaskFocus();
                        return;
                    }
                    else if (Keyboard.IsKeyDown(Key.Up))
                    {
                        PreviousTaskFocus();
                        int limit = TaskStack.Children.Count;
                        do
                        {
                            currentFocus--;
                            if (currentFocus.Value < 0) currentFocus = TaskStack.Children.Count - 1;
                            if (--limit <= 0)
                            {
                                currentFocus = null;
                                return;
                            }
                        } while (TaskStack.Children[currentFocus.Value] is IndividualTask);
                        NextTaskFocus();
                        return;
                    }
                }

                switch (e.Key)
                {
                    case Key.Up:
                        PreviousTaskFocus();
                        return;

                    case Key.Down:
                        NextTaskFocus();
                        return;

                    case Key.Space:
                        ToggleTaskCompletion(task);
                        return;

                    case Key.L:
                        task.LinkOpen();
                        return;

                    case Key.D0:
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) task.Increment_MouseDown("none", null);
                        else task.Increment_MouseDown("now", null);
                        return;

                    case Key.D1:
                        task.Increment_MouseDown("plus1m", null);
                        return;

                    case Key.D2:
                        task.Increment_MouseDown("plus10m", null);
                        return;

                    case Key.D3:
                        task.Increment_MouseDown("plus30m", null);
                        return;

                    case Key.D4:
                        task.Increment_MouseDown("plus1h", null);
                        return;

                    case Key.D5:
                        task.Increment_MouseDown("plus5m", null);
                        return;

                    case Key.D6:
                        task.Increment_MouseDown("plus6h", null);
                        return;

                    case Key.D7:
                        task.Increment_MouseDown("plus1w", null);
                        return;

                    case Key.G:
                        task.Garble(null, TaskStackScroller);
                        return;

                    case Key.E:
                    case Key.Enter:
                        mainWindow.FrameView.Navigate(new EditTaskPage(task));
                        return;

                    case Key.D:
                    case Key.Delete:
                        TrashIcon_MouseDown(task);
                        if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = TaskStack.Children.Count - 1;
                        FocusTask();
                        return;

                    case Key.C:
                        dateClipboard = task.DueDT;
                        return;

                    case Key.V:
                        if (dateClipboard.HasValue)
                        {
                            task.DueDT = dateClipboard;
                            TaskFile.SaveData();
                            task.DueDateTimeLabelUpdate();
                            task.NewDueDT();
                        }
                        return;

                    case Key.Z:
                        lastTask.Last().StrokeOff();
                        TaskFile.TaskList.Add(lastTask.Last());
                        TaskFile.SaveData();
                        lastTask.Remove(lastTask.Last());
                        GenerateTaskStack();
                        return;
                }
            }

            else
                switch (e.Key)
                {
                    case Key.Up:
                        currentFocus = 0;
                        PreviousTaskFocus();
                        return;

                    case Key.Down:
                        currentFocus = 0;
                        FocusTask();
                        return;
                }
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused || Keyboard.IsKeyDown(Key.LWin)) return;
            if (e.Key == Key.S || e.Key == Key.OemQuestion)
                SearchTextBox.Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchTextBox.IsMouseDirectlyOver && !SearchBorder.IsMouseDirectlyOver)
            {
                HomePagePage.Focus();
                if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
            }
            currentFocus = null;
            UnfocusTasks();
        }

        private void Window_Unhidden()
        {
            currentFocus = null;
            UnfocusTasks();
            GenerateTaskStack();
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private async void NotifButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NotifButton.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            NotifIcon.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));

            if (NotifButton.IsMouseOver)
            {
                NotifIcon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    NotifIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    NotifIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        private void NotifButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskFile.NotificationsOn = !TaskFile.NotificationsOn;
            notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(TaskFile.NotificationsOn ? 1 : 0, TimeSpan.FromMilliseconds(250)));
            TaskFile.SaveData();
        }

        private void SearchBorder_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!SearchTextBox.IsMouseOver && !SearchTextBox.IsFocused && SearchTextBox.Text.Length == 0)
                RunSearchTextBoxCloseAnimation((bool)e.NewValue);
        }

        private void SearchBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GenerateTaskStack();
        }

        private async void RunSearchTextBoxCloseAnimation(bool open = false)
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

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TaskFile.sortType = SortComboBox.SelectedIndex;
            TaskFile.SaveData();
            GenerateTaskStack();
        }

        private void ReverseButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            reverseSort = !reverseSort;
            int temp = reverseSort ? 1 : -1;

            DoubleAnimation ani = new(temp, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new QuarticEase()
                {
                    EasingMode = EasingMode.EaseInOut
                }
            };

            SortButton.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = -(temp) };
            SortButton.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);

            GenerateTaskStack();
        }

        private void AddButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UnfocusTasks();
            TaskStack.Children.Clear();
            mainWindow.FrameView.Navigate(new AddTaskPage());
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
            }
            catch { }
        }

        private void GenerateTaskStack()
        {
            if (TaskStack == null) return;

            TaskStack.Children.Clear();

            Dictionary<IndividualTask, object> matchesSort = [], completed = [], sortedDict = [];
            ArrayList doesntMatchSort = [];

            days.Clear();
            int dueTasks = 0;

            Regex regex = new(SearchTextBox.Text.ToLower());
            switch (SortComboBox.SelectedIndex)
            {
                case 1:
                case 2:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (SearchTextBox.Text.Equals("$n", StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (!task.DueDT.HasValue)
                            {
                                if (task.IsCompleted) completed[task] = task.CompletedDT.Value;
                                else doesntMatchSort.Add(task);
                            }
                        }
                        else if (regex.Match(task.TaskName.ToLower()).Success)
                        {
                            if (task.IsCompleted)
                                completed[task] = task.CompletedDT.Value;
                            else
                            {
                                if (SortComboBox.SelectedIndex == 1)
                                    matchesSort[task] = task.CreatedDT.Value;
                                else
                                {
                                    if (task.DueDT.HasValue)
                                        matchesSort[task] = task.DueDT.Value;
                                    else doesntMatchSort.Add(task);
                                }

                                if (task.IsDue) dueTasks++;
                            }
                        }

                    DueTaskCount.Content = (dueTasks == 0) ? "" : $"{dueTasks}d.";
                    TaskCount.Content = $"{matchesSort.Count + doesntMatchSort.Count}t.{completed.Count}c";

                    if (reverseSort)
                    {
                        sortedDict = matchesSort.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                        completed = completed.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                        doesntMatchSort.Reverse();
                    }
                    else
                    {
                        sortedDict = matchesSort.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                        completed = completed.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                    }

                    foreach (IndividualTask task in sortedDict.Keys)
                    {
                        DateTime date = (SortComboBox.SelectedIndex == 1) ? task.CreatedDT.Value : task.DueDT.Value;
                        string dateString = date.ToString("dddd, d") + DTHelper.GetDaySuffix(date.Day) + date.ToString(" MMMM yyyy");

                        if (!days.ContainsKey(dateString))
                        {
                            days[dateString] = [];

                            SectionDivider sectionDivider = new(dateString);
                            if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                            sectionDivider.MouseDown += Section_MouseDown;

                            TaskStack.Children.Add(sectionDivider);
                        }

                        TaskStack.Children.Add(task);
                        days[dateString].Add(task);
                    }

                    if (doesntMatchSort.Count > 0)
                    {
                        days["No date"] = [];

                        SectionDivider sectionDivider = new("No date");
                        if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                        sectionDivider.MouseDown += Section_MouseDown;

                        TaskStack.Children.Add(sectionDivider);

                        foreach (IndividualTask task in doesntMatchSort)
                        {
                            TaskStack.Children.Add(task);
                            days["No date"].Add(task);
                        }
                    }

                    if (completed.Count > 0)
                    {
                        days["Completed"] = [];

                        SectionDivider sectionDivider = new("Completed");
                        if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                        sectionDivider.MouseDown += Section_MouseDown;

                        TaskStack.Children.Add(sectionDivider);

                        foreach (IndividualTask task in completed.Keys)
                        {
                            TaskStack.Children.Add(task);
                            days["Completed"].Add(task);
                            task.Disappear();
                        }

                        sectionDivider.Background_MouseDown(null, null);
                    }

                    break;

                default:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (regex.Match(task.TaskName.ToLower()).Success)
                            if (task.IsCompleted)
                                completed[task] = task.CreatedDT.Value;
                            else
                                matchesSort[task] = task.TaskName;

                    if (reverseSort)
                    {
                        sortedDict = matchesSort.OrderByDescending(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                        completed = completed.OrderByDescending(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                    }
                    else
                    {
                        sortedDict = matchesSort.OrderBy(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                        completed = completed.OrderBy(pair => (pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                    }

                    foreach (IndividualTask task in sortedDict.Keys) TaskStack.Children.Add(task);

                    days["Completed"] = [];
                    SectionDivider sectionDividerDefault = new("Completed");
                    if (TaskStack.Children.Count > 0) sectionDividerDefault.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                    sectionDividerDefault.MouseDown += Section_MouseDown;
                    TaskStack.Children.Add(sectionDividerDefault);

                    foreach (IndividualTask task in completed.Keys)
                    {
                        TaskStack.Children.Add(task);
                        days["Completed"].Add(task);
                        task.Disappear();
                    }

                    sectionDividerDefault.Background_MouseDown(null, null);
                    break;
            }
        }

        private async void Section_MouseDown(object sender, MouseEventArgs e)
        {
            ArrayList aL;
            string? _ = ((SectionDivider)sender).SectionTitle.Content.ToString();
            if (_ == null) return;
            else aL = days[_];

            if (aL.Count <= 0 || aL[0] == null) return;

            if (((IndividualTask)aL[0]).IsVisible)
            {
                foreach (IndividualTask task in aL)
                {
                    task.Disappear();
                    await Task.Delay(60);
                }
            }

            else
            {
                foreach (IndividualTask task in aL)
                {
                    await Task.Delay(60);
                    task.Appear();
                    if (task.IsCompleted)
                    {
                        task.UpdateLayout();
                        task.UpdateTaskCheckBoxAndBackground();
                    }
                }
            }
        }

        private void UpdateTaskTimers(object sender, EventArgs e)
        {
            foreach (IndividualTask task in TaskFile.TaskList)
                task.NewDueDT();
        }

        private void PreviousTaskFocus()
        {
            int limit = TaskStack.Children.Count;
            do
            {
                currentFocus--;
                if (currentFocus.Value < 0) currentFocus = TaskStack.Children.Count - 1;
                if (--limit <= 0)
                {
                    currentFocus = null;
                    return;
                }
            } while (!(TaskStack.Children[currentFocus.Value] is IndividualTask task1 && task1.Visibility == Visibility.Visible));
            FocusTask();
        }

        private void NextTaskFocus()
        {
            int limit = TaskStack.Children.Count;
            do
            {
                currentFocus++;
                if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = 0;
                if (--limit <= 0)
                {
                    currentFocus = null;
                    return;
                }
            } while (TaskStack.Children[currentFocus.Value] is not IndividualTask && limit > 0);

            FocusTask();
        }

        private async void FocusTask()
        {
            if (!currentFocus.HasValue) return;

            UnfocusTasks();

            int count = TaskStack.Children.Count;
            if (count > 0)
            {
                if (!(currentFocus.Value > 0 && currentFocus.Value < count)) currentFocus = 0;

                int limit = count;
                while (currentFocus.Value >= count || !(TaskStack.Children[currentFocus.Value] is IndividualTask task1 && task1.Visibility == Visibility.Visible))
                {
                    if (currentFocus.Value >= count) currentFocus = 0;
                    currentFocus++;
                    if (--limit <= 0)
                    {
                        currentFocus = null;
                        return;
                    }
                }

                IndividualTask task = (IndividualTask)TaskStack.Children[currentFocus.Value];

                double verticalOffset = TaskStackScroller.VerticalOffset;

                // DueTaskCount.Content = task.TransformToVisual(TaskStack.Children[0]).Transform(new Point(0, 0)).Y.ToString();

                task.BringIntoView();
                await Task.Delay(1);

                // TaskCount.Content = TaskStackScroller.VerticalOffset.ToString();

                if (verticalOffset < TaskStackScroller.VerticalOffset)
                    TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset + 50);
                else if (verticalOffset > TaskStackScroller.VerticalOffset)
                    TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset - 50);

                task.StrokeOn();
            }
        }

        private void UnfocusTasks()
        {
            foreach (object obj in TaskStack.Children) if (obj is IndividualTask task) task.StrokeOff();
        }
        private void ToggleTaskCompletion(IndividualTask task)
        {
            task.ToggleCompletionStatus();

            int temp = TaskStack.Children.IndexOf(task);
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
            lastTask.Add(task);
            TaskFile.TaskList.Remove(task);
            TaskFile.SaveData();
            GenerateTaskStack();
        }
    }
}
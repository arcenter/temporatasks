using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public DispatcherTimer UpdateTaskTimersTimer = new() { Interval = TimeSpan.FromSeconds(100) };

        int? currentFocus = null;
        
        bool reverseSort = false;
        bool garbleMode = false;
        Dictionary<string, ArrayList> days = [];

        DateTime? dateClipboard = null;
        List<IndividualTask> lastTask = [];

        IndividualTask hoveredTask;

        private enum ViewCategory
        {
            Home,
            Completed
        }

        ViewCategory currentViewCategory = ViewCategory.Home;

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
                    task.MouseEnter += TaskMouseEnter;
                }

                GenerateTaskStack();
                if (currentFocus.HasValue) FocusTask();
            }

            UpdateNotificationMode();
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
                task.MouseEnter -= TaskMouseEnter;
            }
            TaskStack.Children.Clear();
        }

        private void RefreshToPage(ViewCategory viewCategory)
        {
            currentViewCategory = viewCategory;
            label.Content = currentViewCategory.ToString();
            currentFocus = null;
            UnfocusTasks();
            HomePagePage.Focus();
            SearchTextBox.Text = "";
            RunSearchTextBoxCloseAnimation();
            GenerateTaskStack();
        }

        private async void RunAnimation(Image Icon)
        {
            Icon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

            {
                DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            }

            await Task.Delay(251);

            {
                DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            }
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                if (Keyboard.IsKeyDown(Key.Up))
                {
                    RunAnimation(HomeIcon);
                    RefreshToPage(ViewCategory.Home);
                }
                else if (Keyboard.IsKeyDown(Key.Down))
                {
                    RunAnimation(CompletedIcon);
                    RefreshToPage(ViewCategory.Completed);
                }
            }

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
                foreach (object obj in TaskStack.Children)
                    if (obj is IndividualTask task)
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

                case Key.X:
                    if (hoveredTask.IsMouseOver)
                    {
                        currentFocus = TaskStack.Children.IndexOf(hoveredTask);
                        UnfocusTasks();
                        FocusTask();
                    }
                    return;

                case Key.H:
                    RunAnimation(HomeIcon);
                    RefreshToPage(ViewCategory.Home);
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
                        currentFocus--;
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
                    else if (Keyboard.IsKeyDown(Key.Left))
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
                        } while (TaskStack.Children[currentFocus.Value] is not SectionDivider);
                        Section_MouseDown(TaskStack.Children[currentFocus.Value], null);
                        
                        currentFocus = null;

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

                    case Key.D8:
                        task.Increment_MouseDown("plus1d", null);
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

                    case Key.C:
                        RunAnimation(CompletedIcon);
                        RefreshToPage(ViewCategory.Completed);
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
                NotifGrid.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    NotifGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    NotifGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        private void NotifButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskFile.notificationMode = (TaskFile.NotificationMode)Enum.Parse(typeof(TaskFile.NotificationMode), ((((int)TaskFile.notificationMode) + 1) % 3).ToString());
            UpdateNotificationMode();
            TaskFile.SaveData();
        }
        private void UpdateNotificationMode()
        {
            if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal)
            {
                if (notifLine.Opacity != 0) notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                if (notifLineHP.Opacity != 0) notifLineHP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
            }
            else if (TaskFile.notificationMode == TaskFile.NotificationMode.High)
            {
                if (notifLine.Opacity != 0) notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                if (notifLineHP.Opacity != 1) notifLineHP.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
            }
            else
            {
                if (notifLine.Opacity != 1) notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                if (notifLineHP.Opacity != 0) notifLineHP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
            }
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

        private void CategoryButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            if ((bool)e.NewValue)
            {
                if (((Border)sender).Name == "HomeButton") RunAnimation(HomeIcon);
                else RunAnimation(CompletedIcon);
            }
        }

        private void CategoryButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (((Border)sender).Name == "HomeButton") RefreshToPage(ViewCategory.Home);
            else RefreshToPage(ViewCategory.Completed);
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
            TaskStackScroller.ScrollToVerticalOffset(0);

            Dictionary<IndividualTask, object> yesDueDate = [], sortedDict = [];
            ArrayList noDueDate = [];

            days.Clear();
            int dueTasks = 0;

            List<IndividualTask> tasks = [];

            if (currentViewCategory == ViewCategory.Completed)
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    if (task.IsCompleted) tasks.Add(task);
                }
            else
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    if (task.IsCompleted) continue;
                    tasks.Add(task);
                }

            Regex regex = new(SearchTextBox.Text.ToLower());
            switch (SortComboBox.SelectedIndex)
            {
                case 1:
                case 2:

                    if (SearchTextBox.Text.Length != 0)
                    {
                        if (SearchTextBox.Text.Equals("$n", StringComparison.CurrentCultureIgnoreCase))
                        {
                            foreach (IndividualTask task in tasks)
                                if (!task.DueDT.HasValue) noDueDate.Add(task);
                        }
                        else
                        {
                            if (SortComboBox.SelectedIndex == 1)
                            {
                                foreach (IndividualTask task in tasks)
                                    if (regex.Match(task.TaskName.ToLower()).Success)
                                    {
                                        yesDueDate[task] = task.CreatedDT.Value;
                                        if (task.IsDue) dueTasks++;
                                    }
                            }

                            else
                                foreach (IndividualTask task in tasks)
                                    if (regex.Match(task.TaskName.ToLower()).Success)
                                    {
                                        if (task.DueDT.HasValue)
                                        {
                                            yesDueDate[task] = task.DueDT.Value;
                                            if (task.IsDue) dueTasks++;
                                        }
                                        else noDueDate.Add(task);
                                    }
                        }
                    }
                    else
                    {
                        if (SortComboBox.SelectedIndex == 1)
                        {
                            foreach (IndividualTask task in tasks)
                            {
                                yesDueDate[task] = task.CreatedDT.Value;
                                if (task.IsDue) dueTasks++;
                            }
                        }

                        else
                            foreach (IndividualTask task in tasks)
                            {
                                if (task.DueDT.HasValue)
                                {
                                    yesDueDate[task] = task.DueDT.Value;
                                    if (task.IsDue) dueTasks++;
                                }
                                else noDueDate.Add(task);
                            }
                    }

                    DueTaskCount.Content = (dueTasks == 0) ? "" : $"{dueTasks}d.";
                    TaskCount.Content = $"{yesDueDate.Count + noDueDate.Count}t";

                    if (reverseSort)
                    {
                        sortedDict = yesDueDate.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                        noDueDate.Reverse();
                    }
                    else
                    {
                        sortedDict = yesDueDate.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
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

                    if (noDueDate.Count > 0)
                    {
                        days["No date"] = [];

                        SectionDivider sectionDivider = new("No date");
                        if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                        sectionDivider.MouseDown += Section_MouseDown;

                        TaskStack.Children.Add(sectionDivider);

                        foreach (IndividualTask task in noDueDate)
                        {
                            TaskStack.Children.Add(task);
                            days["No date"].Add(task);
                        }
                    }

                    break;

                default:

                    if (SearchTextBox.Text.Length != 0)
                    {
                        if (SearchTextBox.Text.Equals("$n", StringComparison.CurrentCultureIgnoreCase))
                        {
                            foreach (IndividualTask task in tasks)
                                if (task.DueDT.HasValue) tasks.Remove(task);
                        }
                        else
                        {
                            foreach (IndividualTask task in tasks)
                            {
                                if (regex.Match(task.TaskName.ToLower()).Success)
                                {
                                    if (task.IsDue) dueTasks++;
                                    continue;
                                }
                                tasks.Remove(task);
                            }
                        }
                    }

                    if (reverseSort)
                        tasks = [.. tasks.OrderByDescending(pair => pair.TaskName)];
                    else
                        tasks = [.. tasks.OrderBy(pair => pair.TaskName)];

                    foreach (IndividualTask task in tasks) TaskStack.Children.Add(task);

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
            double taskTimeRemaining;
            foreach (IndividualTask task in TaskFile.TaskList)
            {
                if (task.IsCompleted || task.IsDue) continue;
                if (task.DueDT.HasValue)
                {
                    taskTimeRemaining = (task.DueDT.Value - DateTime.Now).TotalSeconds;
                    if (taskTimeRemaining < TimeSpan.FromHours(1).TotalSeconds) task.NewDueDT();
                }
            }
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
        }

        private void TaskMouseEnter(object sender, MouseEventArgs e)
        {
            hoveredTask = (IndividualTask)sender;
        }
    }
}
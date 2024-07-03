using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TemporaTasks.Core;
using TemporaTasks.UserControls;
using static TemporaTasks.UserControls.IndividualTask;

namespace TemporaTasks.Pages
{
    public partial class HomePage : Page
    {
        private readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        public DispatcherTimer UpdateTaskTimersTimer = new() { Interval = TimeSpan.FromSeconds(100) };

        int? currentFocus = null;

        bool reverseSort = false;
        Dictionary<string, ArrayList> days = [];

        DateTime? dateClipboard = null;
        List<IndividualTask> lastTask = [];

        IndividualTask? hoveredTask = null;
        IndividualTask? editedTask = null;
        MuteModeRightClickMenu muteModeRightClickMenu;

        private enum ViewCategory
        {
            Home = 0,
            Completed = 1,
            WontDo = 2,
            Trash = 3
        }

        ViewCategory currentViewCategory = ViewCategory.Home;

        List<IndividualTask> displayedTasks = [];
        List<IndividualTask> focusedTasks = [];

        public HomePage()
        {
            InitializeComponent();
            UpdateTaskTimersTimer.Tick += UpdateTaskTimers;
            UpdateTaskTimersTimer.Start();
            TaskFile.NotificationModeTimer.Tick += (s, e) => UpdateNotificationTimer(TaskFile.NotificationMode.Normal);
            muteModeRightClickMenu = new(RightClickMenuPopup);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SortComboBox.SelectedIndex = TaskFile.sortType;
            mainWindow.KeyDown += Page_KeyDown;
            mainWindow.KeyUp += Page_KeyUp;
            mainWindow.MouseDown += Window_MouseDown;
            mainWindow.IsWindowUnHidden += Window_Unhidden;

            if (TaskFile.TaskList.Count == 0)
                NewTaskArrow.Visibility = Visibility.Visible;

            else
            {
                NewTaskArrow.Visibility = Visibility.Collapsed;
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    task.IsTrashIconClicked += TrashIcon_MouseDown;
                    task.IsEditIconClicked += EditIcon_MouseDown;
                    task.MouseEnter += TaskMouseEnter;
                    task.MouseDown += TaskMouseDown;
                }

                GenerateTaskStack(false);

                await Task.Delay(750);
                if (currentFocus.HasValue) FocusTask();
            }

            UpdateNotificationMode();
            UpdateNotifPopupMode();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
            mainWindow.KeyUp -= Page_KeyUp;
            mainWindow.MouseDown -= Window_MouseDown;
            mainWindow.IsWindowUnHidden -= Window_Unhidden;

            foreach (IndividualTask task in focusedTasks)
                task.StrokeOff();

            //foreach (IndividualTask task in TaskFile.TaskList)
            //{
            //    task.IsTrashIconClicked -= TrashIcon_MouseDown;
            //    task.IsEditIconClicked -= EditIcon_MouseDown;
            //    task.MouseEnter -= TaskMouseEnter;
            //}

            TaskStack.Children.Clear();
        }

        private void RefreshToPage(ViewCategory viewCategory)
        {
            currentViewCategory = viewCategory;
            label.Content = currentViewCategory.ToString();
            foreach (Image icon in new[] { HomeIcon, CompletedIcon, WontDoIcon, TrashIcon })
                icon.BeginAnimation(OpacityProperty, new DoubleAnimation(($"{viewCategory}Icon" == icon.Name) ? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));

            currentFocus = null;
            UnfocusTasks();

            SearchTextBox.Text = "";
            RunSearchTextBoxCloseAnimation();

            homePage.Focus();
            GenerateTaskStack();
        }

        public void newFileLoaded()
        {
            SortComboBox.SelectedIndex = TaskFile.sortType;
            UpdateNotificationMode();
            UpdateNotifPopupMode();
            GenerateTaskStack(force: true);
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
            if (RightClickMenuPopup.IsOpen) return;

            if (SearchTextBox.IsFocused)
            {
                if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape)
                {
                    homePage.Focus();
                    if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
                    if (e.Key == Key.Enter || e.Key == Key.Tab)
                    {
                        currentFocus = 0;
                        FocusTask();
                    }
                }
                return;
            }

            if ((Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                int? nextViewCategory = null;

                if (Keyboard.IsKeyDown(Key.Up)) nextViewCategory = (((int)currentViewCategory) - 1);
                else if (Keyboard.IsKeyDown(Key.Down)) nextViewCategory = (((int)currentViewCategory) + 1);

                if (nextViewCategory.HasValue)
                {
                    nextViewCategory %= 4;
                    if (nextViewCategory == -1) nextViewCategory = 3;
                    RunAnimation((new[] { HomeIcon, CompletedIcon, WontDoIcon, TrashIcon })[nextViewCategory.Value]);
                    RefreshToPage((ViewCategory)Enum.Parse(typeof(ViewCategory), nextViewCategory.Value.ToString()));
                }
            }

            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (Keyboard.IsKeyDown(Key.R))
                {
                    GenerateTaskStack(force: true);
                    return;
                }

                if (Keyboard.IsKeyDown(Key.OemTilde))
                {
                    EyeButton_MouseDown(null, null);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.D1))
                {
                    SetTempGarble(TempGarbleMode.Off);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.D2))
                {
                    SetTempGarble(TempGarbleMode.On);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.D3))
                {
                    SetTempGarble(TempGarbleMode.None);
                    return;
                }

                //else if (Keyboard.IsKeyDown(Key.D5))
                //{
                //    foreach (IndividualTask task in displayedTasks)
                //        if (task.DueDT.HasValue && task.DueDT.Value.Date == DateTime.Now.Date)
                //            ChangeFocusTaskDueTime("plus5m", null);
                //        else
                //            return;
                //    return;
                //}

                else if (Keyboard.IsKeyDown(Key.M))
                {
                    OpenMuteModeRightClickMenuPopup();
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.I))
                {
                    TaskFile.ImportTasks();
                    GenerateTaskStack(false);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.O))
                {
                    TaskFile.OpenTasksFile();
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.S))
                {
                    TaskFile.SaveTasksFile();
                    return;
                }
            }

            //else if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            //{
            //    if (Keyboard.IsKeyDown(Key.G))
            //    {
            //        foreach (IndividualTask task in displayedTasks)
            //        {
            //            GeneralTransform transform = task.TransformToAncestor(TaskStackScroller);
            //            bool isVisible = (transform.Transform(new Point(task.ActualWidth, task.ActualHeight)).Y >= -50
            //                           && transform.Transform(new Point(0, 0)).Y <= TaskStackScroller.ViewportHeight);
            //            task.Garble(null, isVisible);
            //        }
            //        return;
            //    }
            //}

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
                    if (hoveredTask != null && hoveredTask.IsMouseOver)
                    {
                        currentFocus = TaskStack.Children.IndexOf(hoveredTask);
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

                case Key.O:
                    NotifPopupButton_MouseDown(null, null);
                    return;

                case Key.Z:
                    UndeleteTask();
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
                if (TaskStack.Children.Count == 0 || TaskStack.Children[currentFocus.Value] is not IndividualTask task)
                {
                    currentFocus = null;
                    return;
                }

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
                    else if (Keyboard.IsKeyDown(Key.C))
                    {
                        Clipboard.SetText(task.Name);
                        return;
                    }
                    else if (Keyboard.IsKeyDown(Key.E))
                    {
                        ExportTasks();
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
                        task.ToggleCompletionStatus();
                        return;

                    case Key.L:
                        task.LinkOpen();
                        return;

                    case Key.D0:
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ChangeFocusTaskDueTime("none");
                        else ChangeFocusTaskDueTime("now");
                        return;

                    case Key.D1:
                        ChangeFocusTaskDueTime("plus1m");
                        return;

                    case Key.D2:
                        ChangeFocusTaskDueTime("plus10m");
                        return;

                    case Key.D3:
                        ChangeFocusTaskDueTime("plus30m");
                        return;

                    case Key.D4:
                        ChangeFocusTaskDueTime("plus1h");
                        return;

                    case Key.D5:
                        ChangeFocusTaskDueTime("plus5m");
                        return;

                    case Key.D6:
                        ChangeFocusTaskDueTime("plus6h");
                        return;

                    case Key.D7:
                        ChangeFocusTaskDueTime("plus1w");
                        return;

                    case Key.D8:
                        ChangeFocusTaskDueTime("plus12h");
                        return;

                    case Key.D9:
                        ChangeFocusTaskDueTime("plus1d");
                        return;

                    case Key.G:
                        foreach (IndividualTask _task in focusedTasks) _task.Garble(null, true);
                        return;
                        
                    case Key.E:
                    case Key.Enter:
                        EditIcon_MouseDown(task);
                        return;

                    case Key.P:
                        task.ToggleHP();
                        return;

                    case Key.D:
                    case Key.Delete:
                        List<IndividualTask> _focusedTasks = new(focusedTasks);
                        foreach (IndividualTask _task in _focusedTasks) TrashIcon_MouseDown(_task);
                        return;

                    case Key.W:
                        task.WontDoTask();
                        return;

                    case Key.C:
                        dateClipboard = task.DueDT;
                        return;

                    case Key.V:
                        if (dateClipboard.HasValue)
                        {
                            foreach (IndividualTask _task in focusedTasks)
                            {
                                _task.DueDT = dateClipboard;
                                _task.DueDateTimeLabelUpdate();
                                _task.NewDueDT();
                            }
                            TaskFile.SaveData();
                        }
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

                    case Key.W:
                        RunAnimation(WontDoIcon);
                        RefreshToPage(ViewCategory.WontDo);
                        return;

                    case Key.T:
                        RunAnimation(TrashIcon);
                        RefreshToPage(ViewCategory.Trash);
                        return;
                }
        }

        private void Page_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchTextBox.IsFocused || Keyboard.IsKeyDown(Key.LWin) || RightClickMenuPopup.IsOpen) return;

            else if (e.Key == Key.S || e.Key == Key.OemQuestion)
                SearchTextBox.Focus();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchTextBox.IsMouseDirectlyOver && !SearchBorder.IsMouseDirectlyOver)
            {
                homePage.Focus();
                if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
            }
            if (hoveredTask != null && (hoveredTask.IsMouseOver || (hoveredTask.RightClickMenuPopup != null && hoveredTask.RightClickMenuPopup.IsMouseOver))) return;
            currentFocus = null;
            UnfocusTasks();
        }

        private void Window_Unhidden()
        {
            currentFocus = null;
            UnfocusTasks();
            if (TaskFile.tempGarbleMode == TempGarbleMode.Off) SetTempGarble(TempGarbleMode.None, true);
            foreach (IndividualTask task in displayedTasks) task.DueDateTimeLabelUpdate();
            GenerateTaskStack(false);
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void TaskMouseDown(object sender, MouseButtonEventArgs e)
        {
            IndividualTask task = (IndividualTask)sender;
            currentFocus = TaskStack.Children.IndexOf(task);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (focusedTasks.Contains(task))
                {
                    task.StrokeOff();
                    focusedTasks.Remove(task);
                }
                else
                {
                    task.StrokeOn();
                    focusedTasks.Add(task);
                }
            } else FocusTask(e.ChangedButton != MouseButton.Middle, false);
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
            if (e == null || e.ChangedButton == MouseButton.Left)
            {
                int _ = (int)TaskFile.notificationMode + 1;
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) _ -= 2;
                if ((_ %= 3) == -1) _ = 2;
                TaskFile.notificationMode = (TaskFile.NotificationMode)Enum.Parse(typeof(TaskFile.NotificationMode), _.ToString());
                UpdateNotificationMode();
                TaskFile.SaveData();
            }
        }

        private void NotifButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right) OpenMuteModeRightClickMenuPopup();
        }

        private void OpenMuteModeRightClickMenuPopup()
        {
            RightClickMenuPopup.Child = muteModeRightClickMenu;
            RightClickMenuPopup.IsOpen = true;
            muteModeRightClickMenu.UpdateNotificationMode += UpdateNotificationTimer;
        }

        private void UpdateNotificationMode()
        {
            if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal)
            {
                if (notifLine.Opacity != 0) notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                if (notifLineHP.Opacity != 0) notifLineHP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                TaskFile.NotificationModeTimer.Stop();
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

        private async void NotifPopupButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            NotifPopupButton.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            NotifPopupIcon.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));

            if (NotifPopupButton.IsMouseOver)
            {
                NotifPopupGrid.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    NotifPopupGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifPopupGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    NotifPopupGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    NotifPopupGrid.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        private void NotifPopupButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e == null || e.ChangedButton == MouseButton.Left)
            {
                TaskFile.notifPopupMode = !TaskFile.notifPopupMode;
                UpdateNotifPopupMode();
                TaskFile.SaveData();
            }
        }

        private void UpdateNotifPopupMode()
        {
            NotifPopupLine.BeginAnimation(OpacityProperty, new DoubleAnimation(TaskFile.notifPopupMode ? 0 : 1, TimeSpan.FromMilliseconds(250)));
        }

        private async void EyeButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EyeButton.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            EyeIcon.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));

            if (EyeButton.IsMouseOver)
            {
                EyeIcon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    EyeIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    EyeIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    EyeIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    EyeIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        private void EyeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int _ = (((int)TaskFile.tempGarbleMode) + (((e != null && e.ChangedButton == MouseButton.Right) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ? -1 : 1)) % 3;
            if (_ == -1) _ = 2;
            SetTempGarble((TempGarbleMode)Enum.Parse(typeof(TempGarbleMode), _.ToString()));
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
            GenerateTaskStack(force: !SearchTextBox.Text.Contains("$t"));
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

        public static bool initialFinished = false;

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (initialFinished)
            {
                TaskFile.sortType = SortComboBox.SelectedIndex;
                TaskFile.SaveData();
                GenerateTaskStack(force: true);
            }
            else
            {
                initialFinished = true;
            }
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

            GenerateTaskStack(force: true);
        }

        private void CategoryButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            if ((bool)e.NewValue)
                RunAnimation((new Dictionary<Border, Image>() { { HomeButton, HomeIcon }, { CompletedButton, CompletedIcon }, { WontDoButton, WontDoIcon }, { TrashButton, TrashIcon } })[(Border)sender]);
        }

        private void CategoryButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RefreshToPage((new Dictionary<Border, ViewCategory>() { { HomeButton, ViewCategory.Home }, { CompletedButton, ViewCategory.Completed }, { WontDoButton, ViewCategory.WontDo }, { TrashButton, ViewCategory.Trash } })[(Border)sender]);
        }

        private void AddButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UnfocusTasks();
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
                if (currentFocus != null)
                {
                    System.Windows.Controls.Primitives.Popup _ = ((IndividualTask)TaskStack.Children[currentFocus.Value]).RightClickMenuPopup;
                    if (_ != null && _.IsOpen) ((TaskRightClickMenu)_.Child).PopupClose();
                }
            }
            catch { }
        }

        private bool generateLock = false;
        private TaskCompletionSource<bool> genTSCompletionSource;

        public async void GenerateTaskStack(bool scrollToTop = true, bool force = false)
        {
            if ((generateLock && !force) || TaskStack == null) return;
            generateLock = true;

            await Task.Delay(250);

            genTSCompletionSource = new();
            mainWindow.Cursor = Cursors.Wait;

            TaskStack.Children.Clear();
            if (scrollToTop) TaskStackScroller.ScrollToVerticalOffset(0);

            days.Clear();
            SearchBorder.BorderBrush = null;
            SearchBorder.BorderThickness = new Thickness(0);

            List<IndividualTask> tasks = [];

            int tasksInHour = 0;

            if (currentViewCategory == ViewCategory.Completed)
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                    if (task.taskStatus == IndividualTask.TaskStatus.Completed) tasks.Add(task);
            }

            else if (currentViewCategory == ViewCategory.Trash)
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                    if (task.taskStatus == IndividualTask.TaskStatus.Deleted)
                    {
                        tasks.Add(task);
                        task.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromTicks(0)));
                        task.Appear(timeSpan: 0);
                    }
            }

            else if (currentViewCategory == ViewCategory.WontDo)
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                    if (task.taskStatus == IndividualTask.TaskStatus.WontDo) tasks.Add(task);
            }

            else
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    if (task.taskStatus == IndividualTask.TaskStatus.Normal)
                    {
                        tasks.Add(task);
                        if (task.DueDT.HasValue && task.DueDT.Value - DateTime.Now < TimeSpan.FromHours(1)) tasksInHour++;
                    }
                }
                TasksInHourLabel.Content = tasksInHour;
            }

            if (SearchTextBox.Text.Length != 0)
            {
                string searchTerm = SearchTextBox.Text.ToLower();

                if (searchTerm.Contains("$n"))
                {
                    for (int i = tasks.Count - 1; i >= 0; i--)
                        if (tasks[i].DueDT.HasValue) tasks.Remove(tasks[i]);
                    searchTerm = searchTerm.Replace("$n", "").Trim();
                }

                if (searchTerm.Contains("$p"))
                {
                    for (int i = tasks.Count - 1; i >= 0; i--)
                        if (tasks[i].taskPriority == TaskPriority.Normal) tasks.Remove(tasks[i]);
                    searchTerm = searchTerm.Replace("$p", "").Trim();
                }

                {
                    Match match;
                    if ((match = new Regex("\\$tb? ([^ ]+) ?").Match(searchTerm)).Success)
                    {
                        if (DateTime.TryParse(match.Value[3..].Trim(), out DateTime compareDT))
                        {
                            if (match.Value[2] == 'b')
                            {
                                for (int i = tasks.Count - 1; i >= 0; i--)
                                    if (tasks[i].DueDT.HasValue && tasks[i].DueDT.Value > compareDT) tasks.Remove(tasks[i]);
                            }
                            else
                            {
                                for (int i = tasks.Count - 1; i >= 0; i--)
                                    if (tasks[i].DueDT.HasValue && tasks[i].DueDT.Value < compareDT) tasks.Remove(tasks[i]);
                            }
                        }
                        else
                        {
                            SearchBorder.BorderBrush = (SolidColorBrush)mainWindow.FindResource("PastDue");
                            SearchBorder.BorderThickness = new Thickness(2);
                        }
                        searchTerm = searchTerm.Replace(match.Value, "").Trim();
                    }
                }

                if (searchTerm.Contains('#'))
                {
                    MatchCollection matches = RegexTags().Matches(searchTerm);
                    if (matches.Count != 0)
                    {
                        for (int i = tasks.Count - 1; i >= 0; i--)
                        {
                            if (tasks[i].TagList != null)
                            {
                                foreach (string tag in tasks[i].TagList)
                                    foreach (Match match in matches)
                                        if ((new Regex(match.Value[1..])).Match(tag).Success) // (tag.Contains(match.Value[1..], StringComparison.CurrentCultureIgnoreCase))
                                            goto NextTask;
                                tasks.Remove(tasks[i]);
                            }
                            else tasks.Remove(tasks[i]);
                            NextTask:;
                        }
                        foreach (Match match in matches)
                            searchTerm = searchTerm.Replace($"{match.Value}", "").Trim();
                        searchTerm = searchTerm.Trim();
                    }
                }

                if (searchTerm.Length > 0)
                {
                    try
                    {
                        Regex regex = new(searchTerm);
                        for (int i = tasks.Count - 1; i >= 0; i--)
                        {
                            if (regex.Match(tasks[i].Name.ToLower()).Success) continue;
                            tasks.Remove(tasks[i]);
                        }
                    }
                    catch { }
                }
            }

            Dictionary<IndividualTask, object> yesDate = [], sortedDict = [];
            List<IndividualTask> noDate = [];
            int dueTasks = 0;

            if (SortComboBox.SelectedIndex == 0)
            {
                if (reverseSort)
                    tasks = [.. tasks.OrderByDescending(pair => pair.Name)];
                else
                    tasks = [.. tasks.OrderBy(pair => pair.Name)];

                TaskCount.Content = $"{tasks.Count}t";
                foreach (IndividualTask task in tasks)
                {
                    TaskStack.Children.Add(task);
                    if (task.IsDue) dueTasks++;
                }

                goto Finally;
            }

            else if (SortComboBox.SelectedIndex == 1)
                foreach (IndividualTask task in tasks)
                    if (task.CreatedDT.HasValue)
                    {
                        yesDate[task] = task.CreatedDT.Value;
                        if (task.IsDue) dueTasks++;
                    }
                    else noDate.Add(task);

            else if (SortComboBox.SelectedIndex == 2)
                foreach (IndividualTask task in tasks)
                    if (task.DueDT.HasValue)
                    {
                        yesDate[task] = task.DueDT.Value;
                        if (task.IsDue) dueTasks++;
                    }
                    else noDate.Add(task);

            if (reverseSort)
            {
                sortedDict = yesDate.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                noDate.Reverse();
            }
            else sortedDict = yesDate.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

            tasks = [.. sortedDict.Keys];
            tasks.AddRange(noDate);

            List<SectionDivider> sectionDividers = [];

            foreach (IndividualTask task in sortedDict.Keys)
            {
                DateTime date = (DateTime)sortedDict[task];
                string dateString = date.ToString("dddd, d") + DTHelper.GetDaySuffix(date.Day) + date.ToString(" MMMM yyyy");

                if (!days.ContainsKey(dateString))
                {
                    days[dateString] = [];

                    SectionDivider sectionDivider = new(dateString);
                    if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                    sectionDivider.MouseDown += Section_MouseDown;
                    sectionDividers.Add(sectionDivider);

                    TaskStack.Children.Add(sectionDivider);
                }

                TaskStack.Children.Add(task);
                days[dateString].Add(task);
            }

            if (noDate.Count > 0)
            {
                days["No date"] = [];

                SectionDivider sectionDivider = new("No date");
                if (TaskStack.Children.Count > 0) sectionDivider.MainGrid.Margin = new Thickness(0, 14, 0, 0);
                sectionDivider.MouseDown += Section_MouseDown;
                sectionDividers.Add(sectionDivider);

                TaskStack.Children.Add(sectionDivider);

                foreach (IndividualTask task in noDate)
                {
                    TaskStack.Children.Add(task);
                    days["No date"].Add(task);
                }
            }

            foreach (SectionDivider sectionDivider in sectionDividers)
            {
                string? _ = sectionDivider.SectionTitle.Content.ToString();
                if (_ == null) continue;
                else
                {
                    sectionDivider.SectionTitle.Content += $" ({days[_].Count})";
                }
            }

            TaskCount.Content = $"{tasks.Count}t";

        Finally:

            DueTaskCount.Content = (dueTasks == 0) ? "" : $"{dueTasks}d.";

            displayedTasks = tasks;

            if (editedTask is not null)
            {
                int? index = null;
                foreach (IndividualTask task in displayedTasks)
                    if (task.UID == editedTask.UID)
                    {
                        index = TaskStack.Children.IndexOf(task);
                        break;
                    }
                if (index.HasValue)
                {
                    currentFocus = index.Value;
                    FocusTask();
                }
                editedTask = null;
            }

            if (currentViewCategory == ViewCategory.Completed)
                for (int i = 0; i < Math.Min(10, tasks.Count); i++)
                    tasks[i].UpdateLayoutAndStrikethrough();

            else UpdateNextDueTask();

            mainWindow.Cursor = Cursors.Arrow;

            generateLock = false;
            try { genTSCompletionSource.SetResult(true); }
            catch { }
        }

        [GeneratedRegex(@"#\S+")]
        public static partial Regex RegexTags();

        private void UpdateNextDueTask()
        {
            if (TaskFile.TaskList.Count == 0) return;

            IndividualTask? nextDueTask = null;

            if (TaskFile.sortType == 2)
            {
                if (reverseSort)
                {
                    for (int i = displayedTasks.Count - 1; i >= 0; i--)
                        if (!displayedTasks[i].IsCompleted && displayedTasks[i].DueDT.HasValue)
                        {
                            nextDueTask = displayedTasks[i];
                            break;
                        }
                }
                else
                {
                    for (int i = 0; i < displayedTasks.Count; i++)
                        if (!displayedTasks[i].IsCompleted && displayedTasks[i].DueDT.HasValue)
                        {
                            nextDueTask = displayedTasks[i];
                            break;
                        }
                }
            }
            else
            {
                for (int i = displayedTasks.Count - 1; i >= 0; i--)
                    if (!displayedTasks[i].IsCompleted && displayedTasks[i].DueDT.HasValue)
                        if (nextDueTask == null) nextDueTask = displayedTasks[i];
                        else if (displayedTasks[i].DueDT < nextDueTask.DueDT) nextDueTask = displayedTasks[i];
            }

            if (nextDueTask == null) StatusGrid.Visibility = Visibility.Collapsed;
            else
            {
                StatusGrid.Visibility = Visibility.Visible;
                if ((nextDueTask.IsGarbled() && TaskFile.tempGarbleMode != TempGarbleMode.Off) || TaskFile.tempGarbleMode == TempGarbleMode.On)
                    NextTaskDueNameLabel.Content = "Garbled Task";
                else
                {
                    NextTaskDueNameLabel.Content = nextDueTask.Name;
                    if (nextDueTask.Name.Length > 20) NextTaskDueNameLabel.Content = $"{nextDueTask.Name[..20].Trim()}...";
                }
                NextTaskDueTimeLabel.Content = DTHelper.GetRelativeTaskDueTime(nextDueTask);
            }
        }

        private async void Section_MouseDown(object sender, MouseEventArgs e)
        {
            ArrayList aL;
            string? _ = ((SectionDivider)sender).Background.Tag.ToString();
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

        public void UpdateNotificationTimer(TaskFile.NotificationMode notificationMode)
        {
            TaskFile.notificationMode = notificationMode;
            UpdateNotificationMode();
            TaskFile.NotificationModeTimer.Stop();
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

        private void ChangeFocusTaskDueTime(string newTime)
        {
            foreach (IndividualTask task in focusedTasks)
                task.ChangeDueTime(newTime, null);
        }

        private void PreviousTaskFocus()
        {
            int limit = TaskStack.Children.Count;
            if (limit > 0) {
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
        }

        private void NextTaskFocus()
        {
            int limit = TaskStack.Children.Count;
            if (limit > 0) {
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
        }

        private async void FocusTask(bool unfocus = true, bool centerTaskOnScreen = true)
        {
            if (!currentFocus.HasValue || TaskStack.Children.Count == 0 || TaskStack.Children.Count < currentFocus.Value) return;

            UnfocusTasks(unfocus);

            int count = TaskStack.Children.Count;
            if (count > 0)
            {
                if (currentFocus.Value < 0 || currentFocus.Value > count) currentFocus = 0;

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

                if (centerTaskOnScreen)
                {
                    double verticalCenter = (TaskStackScroller.ActualHeight / 2) - task.ActualHeight;
                    double relativeHeight = task.TransformToVisual(TaskStackScroller).Transform(new Point(0, 0)).Y;
                    TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset + relativeHeight - verticalCenter);
                }

                task.StrokeOn();
                if (!focusedTasks.Contains(task)) focusedTasks.Add(task);
            }
        }

        private void UnfocusTasks(bool unfocus = true)
        {
            if (unfocus && !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                foreach (IndividualTask task in focusedTasks) task.StrokeOff();
                focusedTasks.Clear();
            }
        }

        private void SetTempGarble(TempGarbleMode tempGarbleMode, bool dontPlayAnimation = false)
        {
            TaskFile.tempGarbleMode = tempGarbleMode;
            EyeIcon.Source = (ImageSource)mainWindow.FindResource($"TempGarble{tempGarbleMode}EyeIcon");
            foreach (IndividualTask task in displayedTasks)
            {
                if (dontPlayAnimation) task.TempGarble(tempGarbleMode, false);
                else
                {
                    GeneralTransform transform = task.TransformToAncestor(TaskStackScroller);
                    bool isVisible = (transform.Transform(new Point(task.ActualWidth, task.ActualHeight)).Y >= -50
                                   && transform.Transform(new Point(0, 0)).Y <= TaskStackScroller.ViewportHeight);
                    task.TempGarble(tempGarbleMode, isVisible);
                }
            }
            UpdateNextDueTask();
        }

        private void EditIcon_MouseDown(object sender)
        {
            editedTask = (IndividualTask)sender;
            mainWindow.FrameView.Navigate(new EditTaskPage((IndividualTask)sender));
        }

        private async void TrashIcon_MouseDown(object sender)
        {
            IndividualTask task = (IndividualTask)sender;
            task.TaskTimer.Stop();
            lastTask.Add(task);

            task.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(500)));
            await Task.Delay(501);

            task.Disappear();
            await Task.Delay(251);

            NextTaskFocus();

            TaskStack.Children.Remove(task);
            currentFocus -= 1;

            if (task.taskStatus == IndividualTask.TaskStatus.Deleted) TaskFile.TaskList.Remove(task);
            else task.taskStatus = IndividualTask.TaskStatus.Deleted;

            TaskFile.SaveData();
        }

        private async void UndeleteTask()
        {
            if (lastTask.Count == 0) return;

            IndividualTask task = lastTask.Last();
            if (task.IsCompleted) task.taskStatus = IndividualTask.TaskStatus.Completed;
            else task.taskStatus = IndividualTask.TaskStatus.Normal;
            task.Visibility = Visibility.Visible;
            TaskFile.TaskList.Add(task);
            TaskFile.SaveData();

            lastTask.Remove(task);
            editedTask = task;

            GenerateTaskStack(false);
            if (genTSCompletionSource != null) await genTSCompletionSource.Task;

            await Task.Delay(500);
            task.Appear();
            await Task.Delay(251);
            task.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(500)));
        }

        private void TaskMouseEnter(object sender, MouseEventArgs e)
        {
            hoveredTask = (IndividualTask)sender;
        }

        public void WindowIsResizing(bool value)
        {
            if (value)
                foreach (IndividualTask task in displayedTasks)
                    task.Visibility = Visibility.Collapsed;
            else
                foreach (IndividualTask task in displayedTasks)
                    task.Visibility = Visibility.Visible;
        }

        private void ExportTasks()
        {
            Dictionary<string, Dictionary<string, string>> temp = [];
            Dictionary<string, string> _temp;

            foreach (IndividualTask task in focusedTasks)
            {
                _temp = [];
                _temp["taskName"] = task.Name;
                _temp["taskDesc"] = task.Desc;
                _temp["createdTime"] = TaskFile.DateTimeToString(task.CreatedDT);
                _temp["dueTime"] = TaskFile.DateTimeToString(task.DueDT);
                _temp["completedTime"] = TaskFile.DateTimeToString(task.CompletedDT);
                _temp["taskStatus"] = ((int)task.taskStatus).ToString();
                _temp["tags"] = (task.TagList == null) ? "" : string.Join(';', task.TagList.ToArray());
                _temp["recurrance"] = task.RecurranceTimeSpan.HasValue ? task.RecurranceTimeSpan.Value.ToString() : "";
                _temp["garbled"] = task.IsGarbled() ? "1" : "0";
                _temp["taskPriority"] = task.taskPriority == IndividualTask.TaskPriority.High ? "1" : "0";
                _temp["attachments"] = (task.Attachments == null) ? "" : string.Join(';', task.Attachments.ToArray());
                temp[task.UID.ToString()] = _temp;
            }

            Clipboard.SetText(JsonSerializer.Serialize(temp));
        }
    }
}
using System.Collections;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Linq;
using TemporaTasks.Core;
using TemporaTasks.UserControls;
using static TemporaTasks.UserControls.IndividualTask;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TemporaTasks.Pages
{
    public partial class HomePage : Page
    {
        private readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        public DispatcherTimer UpdateTaskTimersTimer = new() { Interval = TimeSpan.FromSeconds(100) };

        int? _currentFocus = null;
        int? currentFocus
        {
            get
            {
                return _currentFocus;
            }
            set
            {
                UnfocusTask();
                _currentFocus = value;
                if (_currentFocus != null)
                {
                    if (_currentFocus < 0) _currentFocus = generatedTasks.Count - 1;
                    else if (_currentFocus > generatedTasks.Count - 1) _currentFocus = 0;
                }
            }
        }

        int _currentPage = 0;
        int currentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                _currentPage = value;
                currentFocus = null;
            }
        }

        bool reverseSort = false;
        Dictionary<string, ArrayList> days = [];

        DateTime? dateClipboard = null;
        List<List<IndividualTask>> deletedTasksList = [];

        IndividualTask? hoveredTask = null;
        IndividualTask? editedTask = null;
        
        MuteModeRightClickMenu muteModeRightClickMenu;
        FilterPopup filterPopup;

        private enum ViewCategory
        {
            Home = 0,
            Completed = 1,
            WontDo = 2,
            Trash = 3
        }

        ViewCategory currentViewCategory = ViewCategory.Home;

        List<IndividualTask> generatedTasks = [];
        List<IndividualTask> selectedTasks = [];

        public HomePage()
        {
            InitializeComponent();
            UpdateTaskTimersTimer.Tick += UpdateTaskTimers;
            UpdateTaskTimersTimer.Start();
            TaskFile.muteNotificationsTimer.Tick += (s, e) => UpdateNotificationTimer(TaskFile.NotificationMode.Normal);
            UpdateNotificationMode(); 
            muteModeRightClickMenu = new(MuteMenuPopup);
            filterPopup = new(FilterMenuPopup);
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
                    task.IsTrashIconClicked += TrashTask;
                    task.IsEditIconClicked += EditIcon_MouseDown;
                    task.MouseEnter += TaskMouseEnter;
                    task.MouseDown += TaskMouseDown;
                }

                GenerateTaskStack(false);

                await Task.Delay(750);
                if (currentFocus.HasValue) FocusCurrent();
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

            UnfocusTask();
            DeselectAll();

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

            SearchTextBox.Text = "";
            RunSearchTextBoxCloseAnimation();

            homePage.Focus();
            currentPage = 0;
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
            if (IsAnyPopupOpen()) return;

            if (PageTextbox.IsFocused)
            {
                if (e.Key == Key.Enter)
                {
                    homePage.Focus();
                    try
                    {
                        currentPage = int.Parse(PageTextbox.Text) - 1;
                        GenerateTaskStack();
                    }
                    catch
                    {
                        PageTextbox.Text = (currentPage+1).ToString();
                    }
                }
                return;
            }

            if (SearchTextBox.IsFocused)
            {
                if (e.Key == Key.Enter || e.Key == Key.Tab || e.Key == Key.Escape)
                {
                    homePage.Focus();
                    if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
                    if (e.Key == Key.Enter || e.Key == Key.Tab)
                    {
                        currentFocus = 0;
                        FocusCurrent();
                    }
                }
                return;
            }

            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                int? nextViewCategory = null;

                if (Keyboard.IsKeyDown(Key.Up))
                {
                    nextViewCategory = ((int)currentViewCategory) - 1;
                    if (nextViewCategory == -1) nextViewCategory = 3;
                }

                else if (Keyboard.IsKeyDown(Key.Down))
                {
                    nextViewCategory = ((int)currentViewCategory) + 1;
                    if (nextViewCategory == 4) nextViewCategory = 0;
                }

                else if (Keyboard.IsKeyDown(Key.D1))
                    nextViewCategory = 0;

                else if (Keyboard.IsKeyDown(Key.D2))
                    nextViewCategory = 1;

                else if (Keyboard.IsKeyDown(Key.D3))
                    nextViewCategory = 2;

                else if (Keyboard.IsKeyDown(Key.D4))
                    nextViewCategory = 3;

                if (nextViewCategory.HasValue)
                {
                    e.Handled = true;
                    RunAnimation((new[] { HomeIcon, CompletedIcon, WontDoIcon, TrashIcon })[nextViewCategory.Value]);
                    RefreshToPage((ViewCategory)Enum.Parse(typeof(ViewCategory), nextViewCategory.Value.ToString()));
                    return;
                }
            }

            else if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (Keyboard.IsKeyDown(Key.R))
                {
                    GenerateTaskStack(force: true);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.OemTilde))
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

                else if (Keyboard.IsKeyDown(Key.A))
                {
                    foreach (IndividualTask task in generatedTasks)
                        TaskSelectedOn(task);
                    return;
                }

                else if (Keyboard.IsKeyDown(Key.D))
                {
                    DeselectAll();
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
                    FocusCurrent();
                    return;

                case Key.End:
                    currentFocus = 0;
                    PreviousTaskFocus();
                    return;

                case Key.K:
                    SearchTextBox.Text = "$n";
                    RunSearchTextBoxCloseAnimation(true);
                    return;

                //case Key.X:
                //    if (hoveredTask != null && hoveredTask.IsMouseOver)
                //    {
                //        ToggleSelected(hoveredTask);
                //        currentFocus = displayedTasks.IndexOf(hoveredTask);
                //        FocusTask();
                //    }
                //    return;

                case Key.H:
                    RunAnimation(HomeIcon);
                    RefreshToPage(ViewCategory.Home);
                    return;

                case Key.S:
                case Key.OemQuestion:
                    currentFocus = null;
                    RunSearchTextBoxCloseAnimation(true);
                    return;

                case Key.R:
                    currentFocus = null;
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
                    UntrashTask();
                    return;

                case Key.Escape:
                    if (currentFocus.HasValue)
                        currentFocus = null;
                    else if (selectedTasks.Count > 0)
                        DeselectAll();
                    else mainWindow.WindowHide();
                    return;

                case Key.Left:
                    currentPage--;
                    GenerateTaskStack();
                    return;

                case Key.Right:
                    currentPage++;
                    GenerateTaskStack();
                    return;
            }

            if (selectedTasks.Count > 0)
            {
                switch (e.Key)
                {
                    case Key.Space:
                        foreach (IndividualTask task in selectedTasks)
                            task.ToggleCompletionStatus();
                        return;

                    case Key.D0:
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) ChangeSelectedTaskDueTime("none");
                        else ChangeSelectedTaskDueTime("now");
                        return;

                    case Key.D1:
                        ChangeSelectedTaskDueTime("plus1m");
                        return;

                    case Key.D2:
                        ChangeSelectedTaskDueTime("plus10m");
                        return;

                    case Key.D3:
                        ChangeSelectedTaskDueTime("plus30m");
                        return;

                    case Key.D4:
                        ChangeSelectedTaskDueTime("plus1h");
                        return;

                    case Key.D5:
                        ChangeSelectedTaskDueTime("plus5m");
                        return;

                    case Key.D6:
                        ChangeSelectedTaskDueTime("plus6h");
                        return;

                    case Key.D7:
                        ChangeSelectedTaskDueTime("plus1w");
                        return;

                    case Key.D8:
                        ChangeSelectedTaskDueTime("plus12h");
                        return;

                    case Key.D9:
                        ChangeSelectedTaskDueTime("plus1d");
                        return;

                    case Key.G:
                        foreach (IndividualTask task in selectedTasks)
                            task.Garble(null, true);
                        return;

                    case Key.P:
                        foreach (IndividualTask task in selectedTasks)
                            task.ToggleHP();
                        return;

                    case Key.D:
                    case Key.Delete:
                        UnfocusTask();
                        currentFocus = null;
                        TrashTask([.. selectedTasks]);
                        DeselectAll();
                        return;

                    case Key.W:
                        foreach (IndividualTask task in selectedTasks)
                            task.WontDoTask();
                        return;

                    case Key.V:
                        if (dateClipboard.HasValue)
                            foreach (IndividualTask task in selectedTasks)
                            {
                                task.dueDT = dateClipboard;
                                task.DueDateTimeLabelUpdate();
                                task.NewDueDT();
                                TaskFile.SaveData();
                            }
                        return;
                }
            }

            if (currentFocus.HasValue)
            {
                if (generatedTasks.Count == 0)
                {
                    currentFocus = null;
                    return;
                }

                IndividualTask task = generatedTasks[currentFocus.Value];

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (Keyboard.IsKeyDown(Key.Down))
                    {
                        ToggleSelected(task);
                        NextTaskFocus();
                        return;
                    }

                    else if (Keyboard.IsKeyDown(Key.Up))
                    {
                        ToggleSelected(task);
                        PreviousTaskFocus();
                        return;
                    }
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    if (Keyboard.IsKeyDown(Key.Down))
                    {
                        int limit = generatedTasks.Count;
                        do
                        {
                            currentFocus++;
                            if (currentFocus.Value > generatedTasks.Count - 1) currentFocus = 0;
                            if (--limit <= 0)
                            {
                                currentFocus = null;
                                return;
                            }
                        } while (generatedTasks[currentFocus.Value] is IndividualTask && limit > 0);
                        NextTaskFocus();
                        return;
                    }
                    else if (Keyboard.IsKeyDown(Key.Up))
                    {
                        currentFocus--;
                        int limit = generatedTasks.Count;
                        do
                        {
                            currentFocus--;
                            if (currentFocus.Value < 0) currentFocus = generatedTasks.Count - 1;
                            if (--limit <= 0)
                            {
                                currentFocus = null;
                                return;
                            }
                        } while (generatedTasks[currentFocus.Value] is IndividualTask);
                        NextTaskFocus();
                        return;
                    }
                    else if (Keyboard.IsKeyDown(Key.C))
                    {
                        Clipboard.SetText(task.name);
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

                    case Key.L:
                        task.LinkOpen();
                        return;

                    case Key.X:
                        ToggleSelected(task);
                        return;

                    case Key.Space:
                        task.ToggleCompletionStatus();
                        return;

                    case Key.D0:
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                            task.ChangeDueTime("none", null);
                        else
                            task.ChangeDueTime("now", null);
                        return;

                    case Key.D1:
                        task.ChangeDueTime("plus1m", null);
                        return;

                    case Key.D2:
                        task.ChangeDueTime("plus10m", null);
                        return;

                    case Key.D3:
                        task.ChangeDueTime("plus30m", null);
                        return;

                    case Key.D4:
                        task.ChangeDueTime("plus1h", null);
                        return;

                    case Key.D5:
                        task.ChangeDueTime("plus5m", null);
                        return;

                    case Key.D6:
                        task.ChangeDueTime("plus6h", null);
                        return;

                    case Key.D7:
                        task.ChangeDueTime("plus1w", null);
                        return;

                    case Key.D8:
                        task.ChangeDueTime("plus12h", null);
                        return;

                    case Key.D9:
                        task.ChangeDueTime("plus1d", null);
                        return;

                    case Key.G:
                        task.Garble(null, true);
                        return;

                    case Key.P:
                        task.ToggleHP();
                        return;

                    case Key.D:
                    case Key.Delete:
                        TrashTask(task);
                        return;

                    case Key.W:
                        task.WontDoTask();
                        return;

                    case Key.V:
                        if (dateClipboard.HasValue)
                        {
                            task.dueDT = dateClipboard;
                            task.DueDateTimeLabelUpdate();
                            task.NewDueDT();
                            TaskFile.SaveData();
                        }
                        return;

                    case Key.E:
                    case Key.Enter:
                        EditIcon_MouseDown(task);
                        return;

                    case Key.C:
                        dateClipboard = task.dueDT;
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
                        FocusCurrent();
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
            if (SearchTextBox.IsFocused || Keyboard.IsKeyDown(Key.LWin) || IsAnyPopupOpen()) return;

            else if (e.Key == Key.S || e.Key == Key.OemQuestion)
                SearchTextBox.Focus();

            else if (e.Key == Key.T)
            {
                if (selectedTasks.Count > 0 || currentFocus.HasValue)
                {
                    if (generatedTasks.Count == 0)
                        return;
                    
                    List<IndividualTask> tasks = (selectedTasks.Count > 0) ? selectedTasks : [generatedTasks[currentFocus.Value]];

                    QuickTimeChangeMenuPopup.Child = new DateTimeChangerPopup(tasks, QuickTimeChangeMenuPopup);

                    double windowWidth = mainWindow.ActualWidth;
                    double windowHeight = mainWindow.ActualHeight;

                    // Calculate the offsets to center the popup
                    double horizontalOffset = (windowWidth + 444) / 2;
                    double verticalOffset = (windowHeight - 90) / 2;

                    // Set the popup's offsets
                    QuickTimeChangeMenuPopup.HorizontalOffset = horizontalOffset;
                    QuickTimeChangeMenuPopup.VerticalOffset = verticalOffset;
                    QuickTimeChangeMenuPopup.Placement = PlacementMode.Relative;
                    QuickTimeChangeMenuPopup.PlacementTarget = mainWindow;

                    QuickTimeChangeMenuPopup.IsOpen = true;
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchTextBox.IsMouseDirectlyOver && !SearchBorder.IsMouseDirectlyOver)
            {
                homePage.Focus();
                if (SearchTextBox.Text.Length == 0) RunSearchTextBoxCloseAnimation();
            }
            if (hoveredTask != null && (hoveredTask.IsMouseOver || (hoveredTask.RightClickMenuPopup != null && hoveredTask.RightClickMenuPopup.IsMouseOver))) return;

            if (e.ChangedButton == MouseButton.Middle && (hoveredTask == null || (hoveredTask != null && !hoveredTask.IsMouseOver)))
                DeselectAll();

            currentFocus = null;
        }

        private void Window_Unhidden()
        {
            currentFocus = null;
            if (TaskFile.tempGarbleMode == TempGarbleMode.Off) SetTempGarble(TempGarbleMode.None, true);
            foreach (IndividualTask task in generatedTasks) task.DueDateTimeLabelUpdate();
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
            currentFocus = generatedTasks.IndexOf(task);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || e.ChangedButton == MouseButton.Middle)
                ToggleSelected(task);
            FocusTask(task, centerTaskOnScreen: false);
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
            MuteMenuPopup.Child = muteModeRightClickMenu;
            MuteMenuPopup.IsOpen = true;
            muteModeRightClickMenu.UpdateNotificationMode += UpdateNotificationTimer;
        }

        public void UpdateNotificationMode()
        {
            if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal)
            {
                if (notifLine.Opacity != 0) notifLine.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                if (notifLineHP.Opacity != 0) notifLineHP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                TaskFile.muteNotificationsTimer.Stop();
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
                TaskFile.popupOnNotification = !TaskFile.popupOnNotification;
                UpdateNotifPopupMode();
                TaskFile.SaveData();
            }
        }

        private void UpdateNotifPopupMode()
        {
            NotifPopupLine.BeginAnimation(OpacityProperty, new DoubleAnimation(TaskFile.popupOnNotification ? 0 : 1, TimeSpan.FromMilliseconds(250)));
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

        private async void FilterButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FilterButton.BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            if (FilterButton.IsMouseOver)
            {
                FilterIcon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    FilterIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    FilterIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    FilterIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    FilterIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        private void FilterButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) OpenFilterMenuPopup();
        }

        private void OpenFilterMenuPopup()
        {
            FilterMenuPopup.Child = filterPopup;
            FilterMenuPopup.IsOpen = true;
            FilterMenuPopup.HorizontalOffset = (filterPopup.ActualWidth - FilterButton.ActualWidth) / 2;
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
            UnfocusTask();
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
                    System.Windows.Controls.Primitives.Popup _ = ((IndividualTask)generatedTasks[currentFocus.Value]).RightClickMenuPopup;
                    if (_ != null && _.IsOpen) ((TaskRightClickMenu)_.Child).PopupClose();
                }
            }
            catch { }
        }

        private bool generateLock = false;
        private TaskCompletionSource<bool> genTSCompletionSource;

        public async void GenerateTaskStack(bool scrollToTop = true, bool force = false)
        {
            void ResetUI()
            {
                foreach (IndividualTask task in generatedTasks)
                {
                    task.StrokeOff();
                    TaskSelectedOff(task);
                }

                TaskStack.Children.Clear();
                if (scrollToTop) TaskStackScroller.ScrollToVerticalOffset(0);

                days.Clear();
                SearchBorder.BorderBrush = null;
                SearchBorder.BorderThickness = new Thickness(0);
            }

            List<IndividualTask> GetCategoryTaskList()
            {
                List<IndividualTask> tasks = [];

                if (currentViewCategory == ViewCategory.Completed)
                {
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.status == IndividualTask.TaskStatus.Completed) tasks.Add(task);
                }

                else if (currentViewCategory == ViewCategory.Trash)
                {
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.status == IndividualTask.TaskStatus.Deleted)
                        {
                            tasks.Add(task);
                            task.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromTicks(0)));
                            task.Appear(timeSpan: 0);
                        }
                }

                else if (currentViewCategory == ViewCategory.WontDo)
                {
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.status == IndividualTask.TaskStatus.WontDo) tasks.Add(task);
                }

                else
                {
                    var tasksInHour = 0;
                    var now = DateTime.Now;
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.status == IndividualTask.TaskStatus.Normal)
                        {
                            tasks.Add(task);
                            if (task.dueDT.HasValue && task.dueDT.Value - now < TimeSpan.FromHours(1)) tasksInHour++;
                        }
                    TasksInHourLabel.Content = tasksInHour;
                }

                return tasks;
            }

            void ApplyFilters(List<IndividualTask> tasks)
            {
                if ((int)filterPopup.PriorityCM.Tag == 1)
                    for (int i = tasks.Count - 1; i >= 0; i--)
                        if (tasks[i].priority == TaskPriority.Normal) tasks.Remove(tasks[i]);

                if ((int)filterPopup.NoDueDateCM.Tag == 1)
                    for (int i = tasks.Count - 1; i >= 0; i--)
                        if (tasks[i].dueDT.HasValue) tasks.Remove(tasks[i]);
            }

            void ApplySearch(List<IndividualTask> tasks)
            {
                if (SearchTextBox.Text.Length == 0) return;

                string searchTerm = SearchTextBox.Text.ToLower();

                {
                    var match = RegexSearchTime().Match(searchTerm);
                    if (match.Success)
                    {
                        try
                        {
                            var compareDT = DTHelper.StringToDateTime(match.Value[3..].Trim(), "");

                            if (match.Value[2] == 'b')
                            {
                                for (int i = tasks.Count - 1; i >= 0; i--)
                                    if (tasks[i].dueDT.HasValue && tasks[i].dueDT.Value > compareDT) tasks.Remove(tasks[i]);
                            }
                            else
                            {
                                for (int i = tasks.Count - 1; i >= 0; i--)
                                    if (tasks[i].dueDT.HasValue && tasks[i].dueDT.Value < compareDT) tasks.Remove(tasks[i]);
                            }
                        }
                        catch (IncorrectDateException)
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
                            if (tasks[i].tagList != null)
                            {
                                foreach (string tag in tasks[i].tagList)
                                    foreach (Match match in matches)
                                        if (Regex.IsMatch(tag, match.Value[1..], RegexOptions.IgnoreCase))
                                            goto NextTask;
                            }
                            tasks.Remove(tasks[i]);
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
                        for (int i = tasks.Count - 1; i >= 0; i--)
                            if (!Regex.IsMatch(tasks[i].name.ToLower(), searchTerm))
                                tasks.Remove(tasks[i]);
                    }
                    catch { }
                }
            }

            if ((generateLock && !force) || TaskStack == null) return;
            generateLock = true;

            genTSCompletionSource = new();
            mainWindow.Cursor = Cursors.Wait;

            ResetUI();
            List<IndividualTask> tasks = GetCategoryTaskList();
            ApplyFilters(tasks);
            ApplySearch(tasks);

            Dictionary<IndividualTask, object> yesDate = [], sortedDict = [];
            List<IndividualTask> noDate = [];
            int dueTasks = 0;

            if (SortComboBox.SelectedIndex == 0)
            {
                if (reverseSort)
                    tasks = [.. tasks.OrderByDescending(pair => pair.name)];
                else
                    tasks = [.. tasks.OrderBy(pair => pair.name)];

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
                    if (task.createdDT.HasValue)
                    {
                        yesDate[task] = task.createdDT.Value;
                        if (task.IsDue) dueTasks++;
                    }
                    else noDate.Add(task);

            else if (SortComboBox.SelectedIndex == 2)
                foreach (IndividualTask task in tasks)
                    if (task.dueDT.HasValue)
                    {
                        yesDate[task] = task.dueDT.Value;
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

            TaskCount.Content = $"{tasks.Count}t";

            if (tasks.Count == 0) goto Finally;

            {
                var MAX_TASK_COUNT = 30;
                if (currentPage < 0) currentPage = 0;
                else if (currentPage > tasks.Count / MAX_TASK_COUNT) currentPage = tasks.Count / MAX_TASK_COUNT;
                PageTextbox.Text = $"{currentPage + 1}";
                var start = currentPage * MAX_TASK_COUNT;
                try { tasks = tasks.GetRange(start, MAX_TASK_COUNT); }
                catch { tasks = tasks.GetRange(start, tasks.Count - start); }
            }

            List<SectionDivider> sectionDividers = [];

            foreach (IndividualTask task in sortedDict.Keys)
            {
                if (!tasks.Contains(task)) continue;

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

                //await Task.Delay(25);
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
                    if (!tasks.Contains(task)) continue;
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

        Finally:

            DueTaskCount.Content = (dueTasks == 0) ? "" : $"{dueTasks}d.";

            generatedTasks = tasks;

            if (editedTask is not null)
            {
                int? index = null;
                foreach (IndividualTask task in generatedTasks)
                    if (task.UID == editedTask.UID)
                    {
                        index = generatedTasks.IndexOf(task);
                        break;
                    }
                if (index.HasValue)
                {
                    currentFocus = index.Value;
                    FocusCurrent();
                }
                editedTask = null;
            }

            if (currentViewCategory == ViewCategory.Completed)
                for (int i = 0; i < Math.Min(10, tasks.Count); i++)
                    tasks[i].UpdateLayoutAndStrikethrough();

            else UpdateNextDueTask();

            mainWindow.Cursor = Cursors.Arrow;

            await Task.Delay(250);

            generateLock = false;
            try { genTSCompletionSource.SetResult(true); }
            catch { }
        }

        [GeneratedRegex(@"#\S+")]
        public static partial Regex RegexTags();

        [GeneratedRegex("\\$tb? ([^ ]+) ?")]
        private static partial Regex RegexSearchTime();

        private void UpdateNextDueTask()
        {
            if (TaskFile.TaskList.Count == 0) return;

            IndividualTask? nextDueTask = null;

            if (TaskFile.sortType == 2)
            {
                if (reverseSort)
                {
                    for (int i = generatedTasks.Count - 1; i >= 0; i--)
                        if (!generatedTasks[i].IsCompleted && generatedTasks[i].dueDT.HasValue)
                        {
                            nextDueTask = generatedTasks[i];
                            break;
                        }
                }
                else
                {
                    for (int i = 0; i < generatedTasks.Count; i++)
                        if (!generatedTasks[i].IsCompleted && generatedTasks[i].dueDT.HasValue)
                        {
                            nextDueTask = generatedTasks[i];
                            break;
                        }
                }
            }
            else
            {
                for (int i = generatedTasks.Count - 1; i >= 0; i--)
                    if (!generatedTasks[i].IsCompleted && generatedTasks[i].dueDT.HasValue)
                        if (nextDueTask == null) nextDueTask = generatedTasks[i];
                        else if (generatedTasks[i].dueDT < nextDueTask.dueDT) nextDueTask = generatedTasks[i];
            }

            if (nextDueTask == null) StatusGrid.Visibility = Visibility.Collapsed;
            else
            {
                StatusGrid.Visibility = Visibility.Visible;
                if ((nextDueTask.IsGarbled() && TaskFile.tempGarbleMode != TempGarbleMode.Off) || TaskFile.tempGarbleMode == TempGarbleMode.On)
                    NextTaskDueNameLabel.Content = "Garbled Task";
                else
                {
                    NextTaskDueNameLabel.Content = nextDueTask.name;
                    if (nextDueTask.name.Length > 20) NextTaskDueNameLabel.Content = $"{nextDueTask.name[..20].Trim()}...";
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
            TaskFile.muteNotificationsTimer.Stop();
        }

        private void UpdateTaskTimers(object sender, EventArgs e)
        {
            double taskTimeRemaining;
            foreach (IndividualTask task in TaskFile.TaskList)
            {
                if (task.IsCompleted || task.IsDue) continue;
                if (task.dueDT.HasValue)
                {
                    taskTimeRemaining = (task.dueDT.Value - DateTime.Now).TotalSeconds;
                    if (taskTimeRemaining < TimeSpan.FromHours(1).TotalSeconds) task.NewDueDT();
                }
            }
        }

        private void ChangeSelectedTaskDueTime(string newTime)
        {
            foreach (IndividualTask task in selectedTasks)
                task.ChangeDueTime(newTime, null);
        }

        private void PreviousTaskFocus()
        {
            if (!currentFocus.HasValue) return;
            currentFocus--;
            FocusCurrent();
        }

        private void NextTaskFocus()
        {
            if (!currentFocus.HasValue) return;
            currentFocus++;
            FocusCurrent();
        }

        private async void FocusCurrent(bool centerTaskOnScreen = true)
        {
            int count = generatedTasks.Count;
            
            if (!currentFocus.HasValue || count == 0) return;
            if (currentFocus.Value < 0 || currentFocus.Value > count-1) currentFocus = 0;

            int limit = count;
            while (!(generatedTasks[currentFocus.Value] is IndividualTask task1 && task1.Visibility == Visibility.Visible))
            {
                currentFocus++;
                if (currentFocus.Value >= count-1) currentFocus = 0;
                if (--limit <= 0)
                {
                    currentFocus = null;
                    return;
                }
            }

            FocusTask(generatedTasks[currentFocus.Value]);
        }

        private void FocusTask(IndividualTask task, bool centerTaskOnScreen = true)
        {
            if (centerTaskOnScreen)
            {
                double verticalCenter = (TaskStackScroller.ActualHeight / 2) - task.ActualHeight;
                double relativeHeight = task.TransformToVisual(TaskStackScroller).Transform(new Point(0, 0)).Y;
                TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset + relativeHeight - verticalCenter);
            }
            task.StrokeOn();
        }

        private void UnfocusTask()
        {
            if (!currentFocus.HasValue || generatedTasks.Count == 0)
                return;
            else
                generatedTasks[currentFocus.Value].StrokeOff();
        }

        private void ToggleSelected(IndividualTask task)
        {
            if (selectedTasks.Contains(task)) TaskSelectedOff(task);
            else TaskSelectedOn(task);
        }

        private void TaskSelectedOff(IndividualTask task)
        {
            selectedTasks.Remove(task);
            task.SelectionBackground.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
            UpdateSelectedCount();
        }

        private void TaskSelectedOn(IndividualTask task)
        {
            if (!selectedTasks.Contains(task))
            {
                selectedTasks.Add(task);
                task.SelectionBackground.BeginAnimation(OpacityProperty, new DoubleAnimation(0.75, TimeSpan.FromMilliseconds(250)));
                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedTasksLabel.Content = $"{selectedTasks.Count} Task{(selectedTasks.Count > 1 ? "s" : "")} Selected";
            SelectedTasksDivider.Visibility = SelectedTasksLabel.Visibility = (selectedTasks.Count > 0 ? Visibility.Visible : Visibility.Collapsed);
        }

        private void DeselectAll()
        {
            while (selectedTasks.Count > 0)
                TaskSelectedOff(selectedTasks[0]);
        }

        private void SetTempGarble(TempGarbleMode tempGarbleMode, bool dontPlayAnimation = false)
        {
            TaskFile.tempGarbleMode = tempGarbleMode;
            EyeIcon.Source = (ImageSource)mainWindow.FindResource($"TempGarble{tempGarbleMode}EyeIcon");
            foreach (IndividualTask task in generatedTasks)
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

        private async void DeleteTask(IndividualTask task)
        {
            task.taskTimer.Stop();

            task.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(500)));
            await Task.Delay(501);

            task.Disappear();
            await Task.Delay(251);

            TaskStack.Children.Remove(task);

            if (task.status == IndividualTask.TaskStatus.Deleted) TaskFile.TaskList.Remove(task);
            else task.status = IndividualTask.TaskStatus.Deleted;
        }

        private void TrashTask(IndividualTask task)
        {
            deletedTasksList.Add([task]);
            DeleteTask(task);
            FocusCurrent();
            TaskFile.SaveData();
        }

        private void TrashTask(List<IndividualTask> tasks)
        {
            deletedTasksList.Add(tasks);
            foreach (IndividualTask task in tasks)
                DeleteTask(task);
            TaskFile.SaveData();
        }

        private async void UndeleteTask(IndividualTask task)
        {
            await Task.Delay(500);
            task.Appear();
            await Task.Delay(251);
            task.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(500)));
        }

        private async void UntrashTask()
        {
            if (deletedTasksList.Count == 0) return;

            foreach (IndividualTask task in deletedTasksList.Last())
            {
                if (task.IsCompleted) task.status = IndividualTask.TaskStatus.Completed;
                else task.status = IndividualTask.TaskStatus.Normal;
                task.Visibility = Visibility.Visible;
                TaskFile.TaskList.Add(task);
                TaskFile.SaveData();

                editedTask = task;
            }

            GenerateTaskStack(false);
            if (genTSCompletionSource != null) await genTSCompletionSource.Task;

            foreach (IndividualTask task in deletedTasksList.Last())
                UndeleteTask(task);

            deletedTasksList.Remove(deletedTasksList.Last());
        }

        private void TaskMouseEnter(object sender, MouseEventArgs e)
        {
            hoveredTask = (IndividualTask)sender;
        }

        public void WindowIsResizing(bool value)
        {
            if (value)
                foreach (IndividualTask task in generatedTasks)
                    task.Visibility = Visibility.Collapsed;
            else
                foreach (IndividualTask task in generatedTasks)
                    task.Visibility = Visibility.Visible;
        }

        private void ExportTasks()
        {
            Dictionary<string, Dictionary<string, string>> temp = [];
            Dictionary<string, string> _temp;

            foreach (IndividualTask task in selectedTasks)
            {
                _temp = [];
                _temp["taskName"] = task.name;
                _temp["taskDesc"] = task.desc;
                _temp["createdTime"] = TaskFile.DateTimeToString(task.createdDT);
                _temp["dueTime"] = TaskFile.DateTimeToString(task.dueDT);
                _temp["completedTime"] = TaskFile.DateTimeToString(task.completedDT);
                _temp["taskStatus"] = ((int)task.status).ToString();
                _temp["tags"] = (task.tagList == null) ? "" : string.Join(';', task.tagList.ToArray());
                _temp["recurrance"] = task.recurranceTS.HasValue ? task.recurranceTS.Value.ToString() : "";
                _temp["garbled"] = task.IsGarbled() ? "1" : "0";
                _temp["taskPriority"] = task.priority == IndividualTask.TaskPriority.High ? "1" : "0";
                _temp["attachments"] = (task.attachments == null) ? "" : string.Join(';', task.attachments.ToArray());
                temp[task.UID.ToString()] = _temp;
            }

            Clipboard.SetText(JsonSerializer.Serialize(temp));
        }

        public bool IsAnyPopupOpen()
        {
            return MuteMenuPopup.IsOpen || QuickTimeChangeMenuPopup.IsOpen || FilterMenuPopup.IsOpen;
        }

        private void PageButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (((Border)sender).Name)
            {
                case "PageUpButton":
                    currentPage--;
                    break;

                case "PageDownButton":
                    currentPage++;
                    break;
            }
            GenerateTaskStack();
        }

        private void PageButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }
    }
}
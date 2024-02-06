using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using TemporaTasks.Core;
using TemporaTasks.UserControls;

namespace TemporaTasks.Pages
{
    public partial class HomePage : Page
    {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        bool focusMode = false;
        int currentFocus = 0;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown += Page_KeyDown;
            if (TaskFile.TaskList.Count == 0)
            {
                NewTaskArrow.Visibility = Visibility.Visible;
            }
            else
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                {
                    task.MouseDown += IndividualTask_MouseDown;
                    task.IsTrashIconClicked += TrashIcon_MouseDown;
                    task.IsEditIconClicked += EditIcon_MouseDown;
                }
                GenerateTaskStack();
                if (focusMode) FocusTask();
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= Page_KeyDown;
            foreach (IndividualTask task in TaskFile.TaskList)
            {
                task.StrokeOff(); // StrokeBorder.BorderThickness = new Thickness(0);
                task.MouseDown -= IndividualTask_MouseDown;
                task.IsTrashIconClicked -= TrashIcon_MouseDown;
                task.IsEditIconClicked -= EditIcon_MouseDown;
            }
            TaskStack.Children.Clear();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.N:
                    AddButton_MouseDown(null, null);
                    break;

                case Key.Escape:
                    if (focusMode)
                    {
                        focusMode = false;
                        UnfocusTasks();
                    }
                    else mainWindow.WindowHide();
                    break;
            }

            if (focusMode)
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
                        ToggleTaskCompletion((IndividualTask)TaskStack.Children[currentFocus]);
                        break;

                    case Key.E:
                    case Key.Enter:
                        mainWindow.FrameView.Navigate(new EditTaskPage((IndividualTask)TaskStack.Children[currentFocus]));
                        break;

                    case Key.D:
                    case Key.Delete:
                        TrashIcon_MouseDown((IndividualTask)TaskStack.Children[currentFocus]);
                        if (currentFocus > TaskStack.Children.Count - 1) currentFocus = TaskStack.Children.Count - 1;
                        FocusTask();
                        break;
                }
            }
            else
                switch (e.Key)
                {
                    case Key.Up:
                        focusMode = true;
                        currentFocus = 0;
                        PreviousTaskFocus();
                        break;

                    case Key.Down:
                        focusMode = true;
                        currentFocus = 0;
                        FocusTask();
                        break;
                }
        }

        private void PreviousTaskFocus()
        {
            currentFocus--;
            if (currentFocus < 0) currentFocus = TaskStack.Children.Count - 1;
            FocusTask();
        }

        private void NextTaskFocus()
        {
            currentFocus++;
            FocusTask();
        }

        private void IndividualTask_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UnfocusTasks();
            focusMode = false;

            ToggleTaskCompletion((IndividualTask)sender);
        }

        private void ToggleTaskCompletion(IndividualTask sender)
        {
            GenerateTaskStack();

            int temp = TaskStack.Children.IndexOf((IndividualTask)sender);
            if (temp > -1) currentFocus = temp;

            TaskFile.SaveData();
        }

        private void EditIcon_MouseDown(object sender)
        {
            mainWindow.FrameView.Navigate(new EditTaskPage((IndividualTask)sender));
        }

        private void TrashIcon_MouseDown(object sender)
        {
            IndividualTask task = (IndividualTask)sender;
            task.TaskTimer.Stop();
            TaskFile.TaskList.Remove(task);
            TaskStack.Children.Remove(task);
            TaskFile.SaveData();
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

        private void AddButton_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)AddButton.ToolTip;
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
                if (!(currentFocus > 0 && currentFocus < count)) currentFocus = 0;
                ((IndividualTask)TaskStack.Children[currentFocus]).StrokeOn();
                ((IndividualTask)TaskStack.Children[currentFocus]).BringIntoView();
            }
        }

        private void UnfocusTasks()
        {
            foreach (IndividualTask task in TaskStack.Children) task.StrokeOff();
        }
        
        private void GenerateTaskStack()
        {
            TaskStack.Children.Clear();

            Dictionary<IndividualTask, object> matchesSort = new(), completed = new(), sortedDict = new();
            ArrayList doesntMatchSort = new();

            switch (SortComboBox.SelectedIndex)
            {
                case 1:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.IsCompleted)
                            completed[task] = task.CreatedDT.Value;
                        else
                            if (task.DueDT.HasValue) matchesSort[task] = task.DueDT.Value;
                        else doesntMatchSort.Add(task);

                    sortedDict = matchesSort.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                    completed = completed.OrderBy(pair => pair.Key).ThenBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                    break;
                default:
                    foreach (IndividualTask task in TaskFile.TaskList)
                        if (task.IsCompleted)
                            completed[task] = task.CreatedDT.Value;
                        else
                            matchesSort[task] = task.TaskName;

                    sortedDict = matchesSort.OrderBy(pair => ((IndividualTask)pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                    completed = completed.OrderBy(pair => ((IndividualTask)pair.Key).TaskName).ToDictionary(pair => pair.Key, pair => pair.Value);
                    break;
            }

            foreach (IndividualTask task in sortedDict.Keys) TaskStack.Children.Add(task);
            foreach (IndividualTask task in doesntMatchSort) TaskStack.Children.Add(task);
            foreach (IndividualTask task in completed.Keys) TaskStack.Children.Add(task);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateTaskStack();
        }
    }
}
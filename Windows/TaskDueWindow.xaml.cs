using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using static TemporaTasks.MainWindow;
using static TemporaTasks.UserControls.IndividualTask;

namespace TemporaTasks.Windows
{
    public partial class TaskDueWindow : Window
    {
        readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        List<IndividualTask> displayedTasks = [];
        List<IndividualTask> focusedTasks = [];

        int? currentFocus = null;

        IndividualTask? hoveredTask = null;
        DateTime? dateClipboard = null;

        public TaskDueWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2; ;
            BeginAnimation(Window.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

            try
            {
                foreach (IndividualTask task in TaskFile.TaskList)
                    if (task.IsDue)
                    {
                        task.MouseEnter += Task_MouseEnter;
                        displayedTasks.Add(task);
                        TaskStack.Children.Add(task);
                    }
            }
            catch
            {
                Close();
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (IndividualTask task in displayedTasks)
                task.MouseEnter -= Task_MouseEnter;
        }

        private void Task_MouseEnter(object sender, MouseEventArgs e)
        {
            hoveredTask = (IndividualTask)sender;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (Keyboard.IsKeyDown(Key.D1))
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
            }

            switch (e.Key)
            {
                case Key.Home:
                    currentFocus = 0;
                    FocusTask();
                    return;

                case Key.End:
                    currentFocus = displayedTasks.Count-1;
                    FocusTask();
                    return;

                case Key.X:
                    if (hoveredTask != null && hoveredTask.IsMouseOver)
                    {
                        currentFocus = TaskStack.Children.IndexOf(hoveredTask);
                        FocusTask();
                    }
                    return;

                case Key.Escape:
                    if (currentFocus.HasValue)
                    {
                        currentFocus = null;
                        UnfocusTasks();
                    }
                    else CloseWindow();
                    return;
            }

            if (currentFocus.HasValue)
            {
                IndividualTask task = (IndividualTask)TaskStack.Children[currentFocus.Value];

                if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.C))
                {
                    Clipboard.SetText(task.Name);
                    return;
                }

                switch (e.Key)
                {
                    case Key.Up:
                        currentFocus--;
                        FocusTask();
                        return;

                    case Key.Down:
                        currentFocus++;
                        FocusTask();
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
                        task.Garble(null, true);
                        return;

                    case Key.P:
                        task.ToggleHP();
                        return;

                    //case Key.D:
                    //case Key.Delete:
                    //    foreach (IndividualTask _task in focusedTasks) { TrashIcon_MouseDown(_task); }
                    //    if (currentFocus.Value > TaskStack.Children.Count - 1) currentFocus = TaskStack.Children.Count - 1;
                    //    FocusTask();
                    //    return;

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
                }
            }

            else
                switch (e.Key)
                {
                    case Key.Up:
                        currentFocus = 0;
                        FocusTask();
                        return;

                    case Key.Down:
                        currentFocus = displayedTasks.Count-1;
                        FocusTask();
                        return;
                }
        }

        private async void CloseWindow()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            await Task.Delay(250);
            UnfocusTasks();
            TaskStack.Children.Clear();
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (hoveredTask != null && (hoveredTask.IsMouseOver || (hoveredTask.RightClickMenuPopup != null && hoveredTask.RightClickMenuPopup.IsMouseOver))) return;
            currentFocus = null;
            UnfocusTasks();
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

        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e) { }

        private async void FocusTask(bool unfocus = true)
        {
            if (!currentFocus.HasValue) return;

            UnfocusTasks(unfocus);

            int count = TaskStack.Children.Count;
            if (count > 0)
            {
                if (currentFocus.Value < 0 || currentFocus.Value > count) currentFocus = 0;

                IndividualTask task = (IndividualTask)TaskStack.Children[currentFocus.Value];

                double verticalOffset = TaskStackScroller.VerticalOffset;

                task.BringIntoView();
                await Task.Delay(1);

                if (verticalOffset < TaskStackScroller.VerticalOffset)
                    TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset + 50);
                else if (verticalOffset > TaskStackScroller.VerticalOffset)
                    TaskStackScroller.ScrollToVerticalOffset(TaskStackScroller.VerticalOffset - 50);

                task.StrokeOn();
                focusedTasks.Add(task);
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

        private void TaskMouseEnter(object sender, MouseEventArgs e)
        {
            hoveredTask = (IndividualTask)sender;
        }

        private void SetTempGarble(TempGarbleMode tempGarbleMode)
        {
            TaskFile.tempGarbleMode = tempGarbleMode;
            EyeIcon.Source = (ImageSource)mainWindow.FindResource($"{tempGarbleMode}EyeIcon");
            foreach (IndividualTask task in displayedTasks)
                task.TempGarble(tempGarbleMode);
        }

        private void ChangeFocusTaskDueTime(string newTime)
        {
            foreach (IndividualTask task in focusedTasks)
                task.ChangeDueTime(newTime, null);
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
    }
}

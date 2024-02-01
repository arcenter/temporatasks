using System.Collections;
using System.Diagnostics;
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
        IndividualTask task;

        public DateTimePicker dtpic;

        public EditTaskPage(IndividualTask task)
        {
            InitializeComponent();

            this.task = task;
            TaskNameTextbox.Text = task.TaskName;
            // DTPicker.Value = task.DueDT;
            dateTextBox.Text = $"{task.DueDT.Value.Year}-{task.DueDT.Value.Month.ToString().PadLeft(2, '0')}-{task.DueDT.Value.Day.ToString().PadLeft(2, '0')}";
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
            Trace.WriteLine(e.Key);
            if (e.Key == Key.Enter && !myPopUp.IsOpen)
            {
                ConfirmButton_MouseDown(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                mainWindow.FrameView.Navigate(new HomePage());
            }
        }

        private void BackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.FrameView.Navigate(new HomePage());
        }

        private void ConfirmButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue)? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void ConfirmButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Nullable<DateTime> newDueDate = null;
            try
            {
                string[] splits = dateTextBox.Text.Split('-');
                int year = int.Parse(splits[0]);
                int month = int.Parse(splits[1]);
                int day = int.Parse(splits[2]);
                newDueDate = new(year, month, day);
            } catch { }

            task.TaskTimer.Stop();
            TaskFile.TaskList[TaskFile.TaskList.IndexOf(task)] = new IndividualTask(TaskNameTextbox.Text, task.CreatedDT, newDueDate, null); // DTPicker.Value
            TaskFile.SaveData();
            mainWindow.FrameView.RemoveBackEntry();
            mainWindow.FrameView.Navigate(new HomePage());
        }

        private void ConfirmButton_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)ConfirmButton.ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void datePicked(object sender, MouseEventArgs e)
        {
            Trace.WriteLine(((ArrayList)((Border)sender).Tag)[0]);
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(Border.OpacityProperty, new DoubleAnimation((bool)e.NewValue? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dtpic = new DateTimePicker(task.DueDT);
            dtpic.textBox = dateTextBox;
            dtpic.popUp = myPopUp;
            myPopUp.Child = dtpic;
            myPopUp.IsOpen = true;
        }
    }
}

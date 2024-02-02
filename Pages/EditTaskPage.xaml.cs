using System.Collections;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
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

            int hour = task.DueDT.Value.Hour;
            string apm;
            if (hour < 12)
            {
                apm = "am";
            }
            else
            {
                apm = "pm";
                hour = hour - 12;
            }

            timeTextBox.Text = $"{hour.ToString().PadLeft(2, '0')}:{task.DueDT.Value.Minute.ToString().PadLeft(2, '0')} {apm}";

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

                int hour, minute;
                if (timeTextBox.Text.Length == 0)
                {
                    hour = 0;
                    minute = 0;
                }
                else
                {
                    splits = timeTextBox.Text.Split(" ");
                    bool am = (splits[1].ToLower() == "am");

                    splits = splits[0].Split(":");
                    hour = int.Parse(splits[0]) + (am ? 0 : 12);
                    minute = int.Parse(splits[1]);
                }

                newDueDate = new(year, month, day, hour, minute, 0);
            }
            catch { return; }

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

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));
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

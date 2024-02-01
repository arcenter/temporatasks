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

        public DateTimePicker dtpic = new DateTimePicker();

        public EditTaskPage(IndividualTask task)
        {
            InitializeComponent();

            this.task = task;
            TaskNameTextbox.Text = task.TaskName;
            // DTPicker.Value = task.DueDT;
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
            if (e.Key == Key.Enter)
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
            task.TaskTimer.Stop();
            TaskFile.TaskList[TaskFile.TaskList.IndexOf(task)] = new IndividualTask(TaskNameTextbox.Text, task.CreatedDT, null, null); // DTPicker.Value
            TaskFile.SaveData();
            Trace.WriteLine($"{dtpic.selectedDateTime[0]}-{dtpic.selectedDateTime[1].ToString().PadLeft(2, '0')}-{dtpic.selectedDateTime[2].ToString().PadLeft(2, '0')}");
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

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dtpic = new DateTimePicker();
            dtpic.textBox = dateTextBox;
            dtpic.popUp = myPopUp;
            myPopUp.Child = dtpic;
            myPopUp.IsOpen = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TemporaTasks.Core;
using TemporaTasks.UserControls;

namespace TemporaTasks.Pages
{
    public partial class EditTaskPage : Page
    {

        readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        IndividualTask task;

        public EditTaskPage(IndividualTask task)
        {
            InitializeComponent();

            this.task = task;
            TaskNameTextbox.Text = task.TaskName;
            DTPicker.Value = task.DueDT;
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
                mainWindow.FrameView.Navigate(new HomeView());
            }
        }

        private void BackIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.FrameView.Navigate(new HomeView());
        }

        private void ConfirmButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue)? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void ConfirmButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskFile.TaskList[TaskFile.TaskList.IndexOf(task)] = new IndividualTask(TaskNameTextbox.Text, task.CreatedDT, DTPicker.Value, null);
            TaskFile.SaveData();
            mainWindow.FrameView.RemoveBackEntry();
            mainWindow.FrameView.Navigate(new HomeView());
        }

        private void ConfirmButton_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)ConfirmButton.ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }
    }
}

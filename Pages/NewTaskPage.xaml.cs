using System;
using System.Collections.Generic;
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
    public partial class NewTaskPage : Page
    {

        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public NewTaskPage()
        {
            InitializeComponent();
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
            if (e.Key == Key.Enter)
            {
                AddButton_MouseDown(null, null);
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

        private void AddButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskFile.TaskList.Add(new IndividualTask(TaskNameTextbox.Text, DateTimeOffset.UtcNow.LocalDateTime, DTPicker.Value, null));
            TaskFile.SaveData();
            mainWindow.FrameView.RemoveBackEntry();
            mainWindow.FrameView.Navigate(new HomeView());
        }

        private void AddButton_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)AddButton.ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }
    }
}
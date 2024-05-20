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

namespace TemporaTasks.Windows
{
    public partial class TaskDueWindow : Window
    {
        public TaskDueWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2; ;
            BeginAnimation(Window.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));

            foreach (IndividualTask task in TaskFile.TaskList)
                if (task.IsDue)
                    TaskStack.Children.Add(task);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) CloseWindow();
        }

        private async void CloseWindow()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            await Task.Delay(250);
            foreach (Object obj in TaskStack.Children)
                if (obj is IndividualTask task && task.IsDue)
                    TaskStack.Children.Remove(task);
            Close();
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

        private void AddButton_MouseDown(object sender, MouseButtonEventArgs e) { }
    }
}

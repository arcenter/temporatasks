using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TemporaTasks.UserControls
{
    public partial class TaskRightClickMenu : UserControl
    {
        Popup popupObject;
        IndividualTask task;

        public TaskRightClickMenu(IndividualTask task, Popup popupObject)
        {
            InitializeComponent();
            this.popupObject = popupObject;
            this.task = task;

            if (!task.IsLinkAvailable()) OpenLinkGrid.Visibility = Visibility.Collapsed;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown += PopupMouseDown;
            mainWindow.KeyDown += PopupKeyDown;
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown -= PopupMouseDown;
            mainWindow.KeyDown -= PopupKeyDown;
        }

        private void PopupMouseDown(object sender, MouseEventArgs e)
        {
            if (IsMouseOver) return;
            PopupClose();
        }

        private void PopupKeyDown(object sender, KeyEventArgs e)
        {
            PopupClose();
        }

        public async void PopupClose(int delay = 200)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(delay)));
            await Task.Delay(delay+25);
            popupObject.IsOpen = false;
            popupObject.Child = null;
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0, TimeSpan.FromMilliseconds(250)));
        }

        private void Delete_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Delete.BeginAnimation(OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            if ((bool)e.NewValue)
            {
                DeleteIcon.Source = (ImageSource)Application.Current.MainWindow.FindResource("redTrashIcon");
                DeleteLabel.Foreground = (SolidColorBrush)Application.Current.MainWindow.FindResource("PastDue");
            }
            else
            {
                DeleteIcon.Source = (ImageSource)Application.Current.MainWindow.FindResource("trashIcon");
                DeleteLabel.Foreground = (SolidColorBrush)Application.Current.MainWindow.FindResource("Text");
            }
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void TimeChange_MouseDown(object sender, MouseButtonEventArgs e)
        {
            task.ChangeDueTime(sender, e);
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (((Border)sender).Name)
            {
                case "Edit":
                    PopupClose();
                    task.EditIcon_MouseDown(null, null);
                    return;

                case "CopyTT":
                    Clipboard.SetText(task.name);
                    PopupClose();
                    return;

                case "WontDo":
                    task.WontDoTask();
                    return;

                case "Garble":
                    task.Garble(null, true);
                    return;

                case "ToggleHP":
                    task.ToggleHP();
                    return;

                case "OpenLink":
                    task.LinkOpen();
                    PopupClose();
                    return;

                case "Delete":
                    PopupClose();
                    task.TrashIcon_MouseDown(null, null);
                    return;

                default:
                    return;
            }
        }
    }
}
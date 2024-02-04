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

        public DateTimePicker datePickerUserControl;

        public EditTaskPage(IndividualTask task)
        {
            InitializeComponent();

            this.task = task;
            TaskNameTextbox.Text = task.TaskName;
            
            if (task.DueDT.HasValue)
            {
                dateTextBox.Text = DTHelper.DateToString(task.DueDT.Value);
                timeTextBox.Text = DTHelper.TimeToString(task.DueDT.Value);
            }

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
            if (e.Key == Key.Enter && !datePickerPopUp.IsOpen)
            {
                ConfirmButton_MouseDown(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                if (datePickerPopUp.IsOpen) datePickerPopUp.IsOpen = false;
                else mainWindow.FrameView.GoBack();
            }
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation((bool)e.NewValue ? 1 : 0.5, TimeSpan.FromMilliseconds(250)));
        }
        
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void calendar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            datePickerUserControl = new DateTimePicker(dateTextBox.Text);
            datePickerUserControl.textBox = dateTextBox;
            datePickerUserControl.popUp = datePickerPopUp;
            datePickerPopUp.Child = datePickerUserControl;
            datePickerPopUp.IsOpen = true;
        }

        private void CancelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            mainWindow.FrameView.GoBack();
        }

        private void ConfirmButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DateBorder.BorderThickness = TimeBorder.BorderThickness = new Thickness(0);
            Nullable<DateTime> newDueDate;
            try
            {
                newDueDate = DTHelper.StringToDateTime(dateTextBox.Text, timeTextBox.Text);
            }
            catch (IncorrectDateException)
            {
                DateBorder.BorderThickness = new Thickness(2);
                return;
            }
            catch (IncorrectTimeException)
            {
                TimeBorder.BorderThickness = new Thickness(2);
                return;
            }

            task.TaskTimer.Stop();
            TaskFile.TaskList[TaskFile.TaskList.IndexOf(task)] = new IndividualTask(TaskNameTextbox.Text, task.CreatedDT, newDueDate, null);
            TaskFile.SaveData();
            mainWindow.FrameView.RemoveBackEntry();
            mainWindow.FrameView.Navigate(new HomePage());
        }

        private void Textbox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }
    }
}
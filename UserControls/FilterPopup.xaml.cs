﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TemporaTasks.UserControls
{
    public partial class FilterPopup : UserControl
    {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        Popup popupObject;

        public FilterPopup(Popup filterPopup)
        {
            InitializeComponent();
            popupObject = filterPopup;

            PriorityCM.Tag = 0;
            DueDateCM.Tag = 0;
            GarbledCM.Tag = 0;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown += FilterPopup_Keydown;
            mainWindow.MouseDown += FilterPopup_MouseDown;
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            mainWindow.KeyDown -= FilterPopup_Keydown;
            mainWindow.MouseDown -= FilterPopup_MouseDown;
        }

        private void FilterPopup_Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                PopupClose();
            }
        }

        private void FilterPopup_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsMouseOver) return;
            PopupClose();
        }

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switch (((Border)sender).Name)
            {
                case "Priority":
                    ToggleCheckMark(PriorityCM, PriorityXM);
                    return;

                case "DueDate":
                    ToggleCheckMark(DueDateCM, DueDateXM);
                    return;

                case "Garbled":
                    ToggleCheckMark(GarbledCM, GarbledXM);
                    return;

                default:
                    return;
            }
        }

        private void ToggleCheckMark(Path checkMark, Grid crossMark)
        {
            checkMark.Tag = ((int)checkMark.Tag + 1) % 3;

            switch ((int)checkMark.Tag)
            {
                case 0:
                    crossMark.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                    break;

                case 1:
                    checkMark.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                    break;

                case 2:
                    checkMark.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                    crossMark.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                    break;

                default:
                    break;
            }

            mainWindow.homePage.GenerateTaskStack(false, true);
        }

        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        public async void PopupClose(int delay = 200)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(delay)));
            await Task.Delay(delay + 25);
            popupObject.IsOpen = false;
            popupObject.Child = null;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TemporaTasks.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace TemporaTasks.UserControls
{
    public partial class MuteModeRightClickMenu : UserControl
    {
        Popup popupObject;

        public MuteModeRightClickMenu(Popup popupObject)
        {
            InitializeComponent();
            this.popupObject = popupObject;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.MouseDown += PopupMouseDown;
            mainWindow.KeyDown += PopupKeyDown;

            if (TaskFile.muteNotificationsTimer.IsEnabled)
            {
                ButtonPanel.Visibility = Visibility.Collapsed;
                t30mLabel.Content = DTHelper.GetRemainingDueTime(TaskFile.muteNotificationsTimerEnd - DateTime.Now);
                t30mLabel.Padding = new Thickness(10, 0, 10, 0);
            }
            else
            {
                ButtonPanel.Visibility = Visibility.Visible;
                t30mLabel.Content = "30m";
                t30mLabel.Padding = new Thickness(0);
            }

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
            if (e.Key == Key.Escape)
            {
                PopupClose();
                return;
            }
            Border_MouseDown(e.Key.ToString() switch
            {
                "D3" => "t30m",
                "D4" => "t1h",
                "D6" => "t6h",
                "D9" => "t1d",
                "D7" => "t1w",
                "T" => "custom",
                _ => ""
            }, null);
            if (e.Key == Key.T)
            {
                BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(100)));
                popupObject.Child = new DateTimeChangerPopup(popupObject);
            }
        }

        public async void PopupClose(int delay = 200)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(delay)));
            await Task.Delay(delay + 25);
            popupObject.IsOpen = false;
            popupObject.Child = null;
        }

        private void Border_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).Background.BeginAnimation(Brush.OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.75 : 0, TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (TaskFile.NotificationModeTimer.IsEnabled)
            //    UpdateNotificationMode.Invoke(TaskFile.NotificationMode.Normal);

            switch ((sender is Border border) ? border.Name : sender)
            {
                case "t30m":
                    TaskFile.muteNotificationsTimer.Interval = TimeSpan.FromMinutes(30);
                    break;

                case "t1h":
                    TaskFile.muteNotificationsTimer.Interval = TimeSpan.FromHours(1);
                    break;

                case "t6h":
                    TaskFile.muteNotificationsTimer.Interval = TimeSpan.FromHours(6);
                    break;

                case "t1d":
                    TaskFile.muteNotificationsTimer.Interval = TimeSpan.FromDays(1);
                    break;

                case "t1w":
                    TaskFile.muteNotificationsTimer.Interval = TimeSpan.FromDays(7);
                    break;

                case "custom":
                    return;

                default:
                    UpdateNotificationMode.Invoke(TaskFile.NotificationMode.Normal);
                    PopupClose();
                    return;
            }

            UpdateNotificationMode.Invoke(TaskFile.NotificationMode.Muted);

            TaskFile.muteNotificationsTimerEnd = DateTime.Now + TaskFile.muteNotificationsTimer.Interval;
            TaskFile.muteNotificationsTimer.Start();
            
            PopupClose();
        }

        public delegate void ResetNotificationModeEvent(TaskFile.NotificationMode notificationMode);
        public event ResetNotificationModeEvent UpdateNotificationMode;
    }
}

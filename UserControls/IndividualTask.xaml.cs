﻿using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TemporaTasks.Core;

namespace TemporaTasks.UserControls
{
    public partial class IndividualTask : UserControl
    {
        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public long TaskUID { get; set; }
        public string TaskName { get; set; }

        private bool completed = false;
        public bool IsCompleted
        {
            get
            {
                return completed;
            }
            set
            {
                completed = value;
                CompletedDT = value ? DateTime.Now : null;
            }
        }

        public Nullable<DateTime> CreatedDT;
        public Nullable<DateTime> DueDT;
        public Nullable<DateTime> CompletedDT;
        
        public DispatcherTimer TaskTimer = new();
        private DispatcherTimer TemporaryRemainingTimer = new();

        public bool IsDue
        {
            get
            {
                if (DueDT.HasValue && !IsCompleted) return ((DueDT.Value - DateTime.Now) < TimeSpan.FromTicks(0));
                return false;
            }
        }

        public IndividualTask(long _TaskUID, String _TaskName, Nullable<DateTime> _CreatedDT, Nullable<DateTime> _DueDT, Nullable<DateTime> _CompletedDT)
        {
            InitializeComponent();

            TaskUID = _TaskUID;
            TaskName = _TaskName;
            taskNameTextBlock.Text = _TaskName;
            
            IsCompleted = _CompletedDT.HasValue;
            
            CreatedDT = _CreatedDT; 
            DueDT = _DueDT;
            CompletedDT = _CompletedDT;

            DueDateTimeLabelUpdate();
            NewDueDT();

            TemporaryRemainingTimer.Interval = TimeSpan.FromSeconds(1);
            TemporaryRemainingTimer.Tick += UpdateDateTimeLabelWithRemaining;
        }

        private void IndividualTask_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTaskCheckBoxAndBackground();
        }

        private void Background_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Background.IsMouseOver)
            {
                if (!Icons.IsMouseOver)
                {
                    TemporaryRemainingTimer.Start();
                    UpdateDateTimeLabelWithRemaining(null, null);
                }
            }
            else
            {
                TemporaryRemainingTimer.Stop();
                DueDateTimeLabelUpdate();
            }
            Background.BeginAnimation(OpacityProperty, new DoubleAnimation(Background.IsMouseOver? 0.2 : 0, TimeSpan.FromMilliseconds(250)));
            if (!Icons.IsMouseOver && !TimeExtend.IsMouseOver) Icons.BeginAnimation(WidthProperty, new DoubleAnimation((Background.IsMouseOver) ? 75 : 0, TimeSpan.FromMilliseconds(250)));
            if (!TimeExtend.IsMouseOver) TimeExtend.BeginAnimation(WidthProperty, new DoubleAnimation(Background.IsMouseOver? ((IsDue && !IsCompleted)? 105 : 0) : 0, TimeSpan.FromMilliseconds(250)));
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleCompletionStatus();
        }

        private void Button_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(Border.OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
        }

        public void ToggleCompletionStatus()
        {
            IsCompleted = !IsCompleted;
            NewDueDT();
            UpdateTaskCheckBoxAndBackground();
        }

        private void UpdateTaskCheckBoxAndBackground()
        {
            checkMark.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted? 1 : 0, TimeSpan.FromMilliseconds(250)));
            taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted? 0.25 : 1, TimeSpan.FromMilliseconds(250)));
            strikethroughLine.BeginAnimation(Line.X2Property, new DoubleAnimation(IsCompleted ? strikethroughLine.MaxWidth : 0, TimeSpan.FromMilliseconds(500)));
            DueDateTimeLabelUpdate();
        }

        public delegate void EditIconClicked(object sender);
        public event EditIconClicked IsEditIconClicked;

        private void EditIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsEditIconClicked?.Invoke(this);
        }

        public delegate void TrashIconClicked(object sender);
        public event TrashIconClicked IsTrashIconClicked;

        private void TrashIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsTrashIconClicked?.Invoke(this);
        }

        private void DueDateTimeLabelUpdate()
        {
            if (DueDT.HasValue)
            {
                string dateString;
                switch (DueDT.Value.Day - DateTime.Now.Day)
                {
                    case 0:
                        dateString = "today";
                        break;
                    case -1:
                        dateString = "yesterday";
                        break;
                    case 1:
                        dateString = "tomorrow";
                        break;
                    default:
                        dateString = DueDT.Value.ToString("dd\\/MM");
                        break;
                }
                if (IsCompleted && CompletedDT.HasValue)
                    DueDateTimeLabel.Content = $"Done {dateString} {CompletedDT.Value.ToString("hh:mm tt")}";
                else
                    DueDateTimeLabel.Content = $"Due {dateString} {DueDT.Value.ToString("hh:mm tt")}";
            }
            DueDateTimeLabel.Foreground = (SolidColorBrush)mainWindow.FindResource((IsDue && !IsCompleted) ? "PastDue": "Text");

            DueDateTimeLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted ? 0.25 : (IsDue ? 1 : 0.5), TimeSpan.FromMilliseconds(250)));
        }

        private void UpdateDateTimeLabelWithRemaining(object sender, EventArgs e)
        {
            if (!DueDT.HasValue) return;
            TimeSpan remainingTime = DueDT.Value - DateTime.Now;
            if (remainingTime <= TimeSpan.FromTicks(0))
            {
                if (!IsCompleted) DueDateTimeLabel.Content = "Past due";
                TemporaryRemainingTimer.Stop();
            }
            else if (remainingTime > TimeSpan.FromMinutes(1))
            {
                double inTime;
                if (remainingTime < TimeSpan.FromHours(1))
                {
                    inTime = remainingTime.TotalMinutes;
                    DueDateTimeLabel.Content = "minute";
                } else if (remainingTime < TimeSpan.FromDays(1))
                {
                    inTime = remainingTime.TotalHours;
                    DueDateTimeLabel.Content = "hour";
                }
                else if (remainingTime < TimeSpan.FromDays(7))
                {
                    inTime = remainingTime.TotalDays;
                    DueDateTimeLabel.Content = "day";
                }
                else if (remainingTime < TimeSpan.FromDays(30))
                {
                    inTime = (remainingTime.TotalDays / 7);
                    DueDateTimeLabel.Content = "week";
                }
                else if (remainingTime < TimeSpan.FromDays(365))
                {
                    inTime = (remainingTime.TotalDays / 30);
                    DueDateTimeLabel.Content = "month";
                } else
                {
                    inTime = (remainingTime.TotalDays / 365);
                    DueDateTimeLabel.Content = "year";
                }

                inTime = Math.Round(inTime, 0);
                if (inTime > 1) DueDateTimeLabel.Content = $"In ~{inTime} {DueDateTimeLabel.Content}s";
                else DueDateTimeLabel.Content = "In a" + (DueDateTimeLabel.Content == "hour" ? "n " : " ") + DueDateTimeLabel.Content;
                TemporaryRemainingTimer.Stop();
            }
            else DueDateTimeLabel.Content = $"In {(int)remainingTime.TotalSeconds} seconds";
        }

        public void StrokeOn()
        {
            StrokeBorder.BorderThickness = new Thickness(3);
        }

        public void StrokeOff()
        {
            StrokeBorder.BorderThickness = new Thickness(0);
        }

        private void NewDueDT()
        {
            TaskTimer.Stop();
            if (DueDT.HasValue && !IsCompleted)
            {
                double taskTimeRemaining = (DueDT.Value - DateTime.Now).TotalSeconds;
                if (TimeSpan.FromSeconds(taskTimeRemaining) > TimeSpan.FromDays(1)) return;
                TaskTimer.Interval = TimeSpan.FromSeconds(Math.Max(2, taskTimeRemaining));
                TaskTimer.Tick += (s, e) =>
                {
                    mainWindow.WindowHide(false);
                    DueDateTimeLabel.Foreground = (SolidColorBrush)mainWindow.FindResource("PastDue");
                    DueDateTimeLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                    mainWindow.TrayIcon.ShowBalloonTip("Task Due!", TaskName, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                    TaskTimer.Interval = TimeSpan.FromMinutes(5);
                };
                TaskTimer.Start();
            }
            DueDateTimeLabelUpdate();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedBorder = sender as Border;
            switch (clickedBorder.Name)
            {
                case "plus5m":
                    DueDT += TimeSpan.FromMinutes(5);
                    break;
                case "plus10m":
                    DueDT += TimeSpan.FromMinutes(10);
                    break;
                case "plus30m":
                    DueDT += TimeSpan.FromMinutes(30);
                    break;
            }
            TaskFile.SaveData();
            DueDateTimeLabelUpdate();
            NewDueDT();
        }
    }
}
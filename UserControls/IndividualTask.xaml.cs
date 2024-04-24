using System.Collections;
using System.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
        readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

        public long TaskUID { get; set; }
        public string TaskName { get; set; }

        public ArrayList? TagList { get; set; }

        private bool completed = false;
        private bool garbled = false;

        public enum TempGarbleMode
        {
            TempGarbleOff = 0,
            TempGarbleOn = 1,
            None = 2
        }

        public enum TaskPriority
        {
            Normal,
            High
        }

        public TaskPriority taskPriority = TaskPriority.Normal;

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
        
        // public Nullable<TimeSpan> RecurranceTimeSpan;
        
        public DispatcherTimer TaskTimer = new();
        readonly private DispatcherTimer TemporaryRemainingTimer = new();

        public bool IsDue
        {
            get
            {
                if (DueDT.HasValue && !IsCompleted) return ((DueDT.Value - DateTime.Now) < TimeSpan.FromTicks(0));
                return false;
            }
        }

        public IndividualTask(long _TaskUID, string _TaskName, Nullable<DateTime> _CreatedDT, Nullable<DateTime> _DueDT, Nullable<DateTime> _CompletedDT, ArrayList? _TagList, Nullable<TimeSpan> _RecurranceTimeSpan, bool _garbled, TaskPriority _taskPriority)
        {
            InitializeComponent();

            TaskUID = _TaskUID;
            
            taskNameTextBlock.Text = TaskName = _TaskName;
            TaskToolTipLabel.Content = (_TaskName.Length > 100) ? ($"{_TaskName[..100]}...") : _TaskName;

            CreatedDT = _CreatedDT;
            DueDT = _DueDT;
            IsCompleted = _CompletedDT.HasValue;
            CompletedDT = _CompletedDT;

            // RecurranceTimeSpan = _RecurranceTimeSpan;
            TagList = _TagList;

            garbled = _garbled;
            if (_taskPriority == TaskPriority.High)
                taskPriority = _taskPriority;

            SetTimer();

            TemporaryRemainingTimer.Interval = TimeSpan.FromSeconds(1);
            TemporaryRemainingTimer.Tick += UpdateDateTimeLabelWithRemaining;
        }

        private void IndividualTask_Loaded(object sender, RoutedEventArgs e)
        {
            TempGarble(TaskFile.tempGarbleMode);
            UpdateTaskCheckBoxAndBackground();
            Cursor = Cursors.Hand;
        }

        private void Background_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!Icons.IsMouseOver)
            {
                TemporaryRemainingTimer.Start();
                UpdateDateTimeLabelWithRemaining(null, null);
            }
            Background.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, TimeSpan.FromMilliseconds(250)));
            Icons.BeginAnimation(WidthProperty, new DoubleAnimation(65, TimeSpan.FromMilliseconds(250)));
        }

        public void Background_MouseLeave(object sender, MouseEventArgs e)
        {
            TemporaryRemainingTimer.Stop();
            DueDateTimeLabelUpdate();
            Background.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
            if (!Icons.IsMouseOver) Icons.BeginAnimation(WidthProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
        }

        private void Background_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(sender as UIElement);
            ToolTip tooltip = (ToolTip)((Border)sender).ToolTip;
            tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            tooltip.HorizontalOffset = mousePosition.X;
            tooltip.VerticalOffset = mousePosition.Y;
        }

        private void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) ToggleCompletionStatus();
        }

        private async void Background_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                if (RightClickMenuPopup.Child != null)
                {
                    ((TaskRightClickMenu)RightClickMenuPopup.Child).PopupClose(100);
                    await Task.Delay(200);
                }
                RightClickMenuPopup.Child = new TaskRightClickMenu(this, RightClickMenuPopup);
                RightClickMenuPopup.IsOpen = true;
            }
        }

        private void Button_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
        }

        public void ToggleCompletionStatus()
        {
            IsCompleted = !IsCompleted;
            TaskFile.SaveData();
            NewDueDT();
            UpdateTaskCheckBoxAndBackground();
        }

        public void UpdateTaskCheckBoxAndBackground()
        {
            checkMark.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted? (taskPriority == TaskPriority.High ? 0.75 : 1) : 0, TimeSpan.FromMilliseconds(250)));
            taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted ? 0.25 : 1, TimeSpan.FromMilliseconds(250)));
            UpdateHP();
            UpdateStrikethrough();
            DueDateTimeLabelUpdate();
        }

        private void UpdateHP()
        {
            Trace.WriteLine($"[{counter++}] Update");
            if (taskPriority == TaskPriority.High)
            {
                CheckBox.BorderBrush = checkMark.Stroke = strikethroughLine.Stroke = (SolidColorBrush)mainWindow.FindResource("HighPriority");
                CheckBox.Opacity = checkMark.Opacity = 0.75;
                strikethroughLine.Opacity = 0.75;
            }
            else
            {
                CheckBox.BorderBrush = (SolidColorBrush)mainWindow.FindResource("CheckBox");
                checkMark.Stroke = (SolidColorBrush)mainWindow.FindResource("Blue");
                strikethroughLine.Stroke = (SolidColorBrush)mainWindow.FindResource("DarkBlue");
                CheckBox.Opacity = strikethroughLine.Opacity = 1;
                checkMark.Opacity = IsCompleted ? 1 : 0;
            }
        }

        public void ToggleHP()
        {
            Trace.WriteLine("Toggle");
            taskPriority = (taskPriority == TaskPriority.Normal) ? TaskPriority.High : TaskPriority.Normal;
            TaskFile.SaveData();
            UpdateHP();
        }

        private async void UpdateStrikethrough()
        {
            await Task.Delay(250);

            DoubleAnimation animation;

            if (IsCompleted && strikethroughLine.Width != strikethroughLine.MaxWidth)
                animation = new DoubleAnimation(0, strikethroughLine.MaxWidth, TimeSpan.FromMilliseconds(500));

            else if (!IsCompleted && strikethroughLine.Width != 0)
                animation = new DoubleAnimation(strikethroughLine.MaxWidth, 0, TimeSpan.FromMilliseconds(500));

            else return;

            Storyboard.SetTarget(animation, strikethroughLine);
            Storyboard.SetTargetProperty(animation, new PropertyPath(WidthProperty));

            Storyboard storyboard = new();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        public void UpdateLayoutAndStrikethrough()
        {
            taskNameTextBlock.UpdateLayout();
            strikethroughLine.UpdateLayout();
            UpdateStrikethrough();
        }

        public delegate void EditIconClicked(object sender);
        public event EditIconClicked IsEditIconClicked;

        public void EditIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsEditIconClicked?.Invoke(this);
        }

        public delegate void TrashIconClicked(object sender);
        public event TrashIconClicked IsTrashIconClicked;

        public void TrashIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TaskTimer.Stop();
            IsTrashIconClicked?.Invoke(this);
        }

        public void DueDateTimeLabelUpdate()
        {
            if (IsCompleted && CompletedDT.HasValue)
            {
                string dateString = (CompletedDT.Value.Date - DateTime.Now.Date).Days switch
                {
                    0 => "",
                    -1 => "yesterday ",
                    1 => "tomorrow ",
                    _ => CompletedDT.Value.ToString("dd\\/MM ")
                };
                DueDateTimeLabel.Content = $"Done {dateString}{CompletedDT.Value:hh:mm tt}";
            }

            else if (DueDT.HasValue)
            {
                string dateString = (DueDT.Value.Date - DateTime.Now.Date).Days switch
                {
                    0 => "",
                    -1 => "yesterday ",
                    1 => "tomorrow ",
                    _ => DueDT.Value.ToString("dd\\/MM ")
                };
                DueDateTimeLabel.Content = $"Due {dateString}{DueDT.Value:hh:mm tt}";
            }

            else
            {
                DueDateTimeLabel.Content = "";
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
                else DueDateTimeLabel.Content = "In a" + ((string)DueDateTimeLabel.Content == "hour" ? "n " : " ") + DueDateTimeLabel.Content;
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

        public void NewDueDT()
        {
            SetTimer();
            DueDateTimeLabelUpdate();
        }

        private void SetTimer()
        {
            TaskTimer.Stop();
            if (DueDT.HasValue && !IsCompleted)
            {
                double taskTimeRemaining = (DueDT.Value - DateTime.Now).TotalSeconds;
                if (taskTimeRemaining <= 120)
                {
                    TaskTimer.Interval = TimeSpan.FromSeconds(Math.Max(2, taskTimeRemaining));
                    TaskTimer.Tick += (s, e) =>
                    {
                        DueDateTimeLabel.Foreground = (SolidColorBrush)mainWindow.FindResource("PastDue");
                        DueDateTimeLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                        if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal)
                            mainWindow.OnTaskDue("Task Due!", garbled ? "Garbled Task" : TaskName, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        else if (TaskFile.notificationMode == TaskFile.NotificationMode.High && taskPriority == TaskPriority.High)
                            mainWindow.OnTaskDue("⚠ Task Due!", garbled ? "Garbled Task" : TaskName, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        TaskTimer.Interval = TimeSpan.FromMinutes(5);
                    };
                    TaskTimer.Start();
                }
            }
        }

        public void ChangeDueTime(object sender, MouseButtonEventArgs e)
        {
            string? name = (sender is Border border) ? border.Name : sender.ToString();

            if (name == "none")
                DueDT = null;
            else if (name == "now")
            {
                DateTime currentDT = DateTime.Now;
                DueDT = new DateTime(currentDT.Year, currentDT.Month, currentDT.Day, currentDT.Hour, DateTime.Now.Minute, 0);
            }
            else
                DueDT = (DueDT ?? DateTime.Now) + ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || (e != null && e.ChangedButton == MouseButton.Right)) ? -1 : 1) * (name) switch
                {
                    "plus1m"  => TimeSpan.FromMinutes(1),
                    "plus5m"  => TimeSpan.FromMinutes(5),
                    "plus10m" => TimeSpan.FromMinutes(10),
                    "plus30m" => TimeSpan.FromMinutes(30),
                    "plus1h"  => TimeSpan.FromHours(1),
                    "plus6h"  => TimeSpan.FromHours(6),
                    "plus12h" => TimeSpan.FromHours(12),
                    "plus1d"  => TimeSpan.FromDays(1),
                    "plus1w"  => TimeSpan.FromDays(7),
                    _ => TimeSpan.FromTicks(0),
                };

            TaskFile.SaveData();
            DueDateTimeLabelUpdate();
            NewDueDT();
        }

        public new bool IsVisible = true;

        public void Appear()
        {
            Visibility = Visibility.Visible;
            ChangeHeight(0, 69);
            IsVisible = true;
        }

        public async void Disappear()
        {
            ChangeHeight(ActualHeight, 0);
            IsVisible = false;
            await Task.Delay(250);
            Visibility = Visibility.Collapsed;
        }

        private void ChangeHeight(double oldValue, double newValue)
        {
            var animation = new DoubleAnimation(oldValue, newValue, TimeSpan.FromMilliseconds(250));
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));

            Storyboard storyboard = new();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        public void LinkOpen()
        {
            Match match = LinkRegex().Match(TaskName);
            if (match.Success)
            {
                Process.Start(new ProcessStartInfo("cmd", "/C start" + " " + match.Value));
            }
        }

        [GeneratedRegex("\\b(https://|www.)\\S+")]
        public static partial Regex LinkRegex();

        public bool IsGarbled()
        {
            return garbled;
        }

        public async void Garble(bool? _garble = null, bool playAnimation = false)
        {
            bool garble = _garble ?? !IsGarbled();

            if (garble == garbled) return;
            garbled = garble;

            if (IsVisible && playAnimation)
            {
                ToolTipService.SetIsEnabled(Background, garble);
                if (garble)
                {
                    strikethroughLine.Visibility = Visibility.Hidden;
                    TextSP.Children.Clear();
                    TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromTicks(0)));

                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                    await Task.Delay(400);
                    taskNameTextBlock.Visibility = Visibility.Hidden;

                    Random random = new();
                    int limit = 3 + random.Next() % 2;

                    for (int i = 0; i < limit; i++)
                    {
                        Line line = new()
                        {
                            X1 = 0,
                            X2 = 0,
                            Stroke = (SolidColorBrush)mainWindow.FindResource("CheckBox"),
                            StrokeThickness = 4,
                            StrokeStartLineCap = PenLineCap.Round,
                            StrokeEndLineCap = PenLineCap.Round,
                            Margin = new Thickness(0, 0, 10, 0),
                            IsHitTestVisible = false
                        };

                        TextSP.Children.Add(line);
                        line.BeginAnimation(Line.X2Property, new DoubleAnimation(50 + random.Next() % 100, TimeSpan.FromMilliseconds(275)));
                        await Task.Delay(300);
                    }
                }
                else
                {
                    TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                    await Task.Delay(400);
                    TextSP.Children.Clear();

                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromTicks(0)));
                    taskNameTextBlock.Visibility = Visibility.Visible;
                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, IsCompleted ? 0.25 : 1, TimeSpan.FromMilliseconds(300)));

                    strikethroughLine.Visibility = Visibility.Visible;
                }
            }

            else TempGarble(garble ? TempGarbleMode.TempGarbleOn : TempGarbleMode.TempGarbleOff);

            TaskFile.SaveData();
        }

        public void TempGarble(TempGarbleMode garbleMode)
        {
            if (garbleMode == TempGarbleMode.TempGarbleOn || (garbleMode == TempGarbleMode.None && garbled))
            {
                if (TextSP.Opacity == 1) return;

                ToolTipService.SetIsEnabled(Background, true);

                strikethroughLine.Visibility = Visibility.Hidden;
                taskNameTextBlock.Visibility = Visibility.Hidden;
                taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromTicks(0)));

                TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromTicks(0)));

                Random random = new();
                int limit = 3 + random.Next() % 2;

                for (int i = 0; i < limit; i++)
                {
                    TextSP.Children.Add(new Line()
                    {
                        X1 = 0,
                        X2 = 50 + random.Next() % 100,
                        Stroke = (SolidColorBrush)mainWindow.FindResource("CheckBox"),
                        StrokeThickness = 4,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        Margin = new Thickness(0, 0, 10, 0),
                        IsHitTestVisible = false
                    });
                }
            }
            else if (garbleMode == TempGarbleMode.TempGarbleOff || (garbleMode == TempGarbleMode.None && !garbled))
            {
                TextSP.Children.Clear();
                ToolTipService.SetIsEnabled(Background, false);

                strikethroughLine.Visibility = Visibility.Visible;
                taskNameTextBlock.Visibility = Visibility.Visible;
                taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, IsCompleted ? 0.25 : 1, TimeSpan.FromTicks(0)));

                TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromTicks(0)));
            }
        }
    }
}
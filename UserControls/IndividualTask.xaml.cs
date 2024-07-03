using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

        public long UID { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }

        public ArrayList? TagList { get; set; }

        public ArrayList? Attachments { get; set; }

        private bool Garbled = false;

        public enum TempGarbleMode
        {
            Off = 0,
            On = 1,
            None = 2
        }

        public enum TaskPriority
        {
            Normal,
            High
        }

        public TaskPriority taskPriority = TaskPriority.Normal;

        public enum TaskStatus
        {
            Normal,
            Completed,
            WontDo,
            Deleted
        }

        public TaskStatus taskStatus = TaskStatus.Normal;

        public bool IsCompleted
        {
            get { return CompletedDT.HasValue; }
            set
            {
                CompletedDT = value ? DateTime.Now : null;
                taskStatus = value ? TaskStatus.Completed : TaskStatus.Normal;
            }
        }

        public DateTime? CreatedDT;
        public DateTime? DueDT;
        public DateTime? CompletedDT;

        public TimeSpan? RecurranceTimeSpan;

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

        public IndividualTask(long _TaskUID, string _TaskName, string _TaskDesc, DateTime? _CreatedDT, DateTime? _DueDT, DateTime? _CompletedDT, TaskStatus _taskStatus, ArrayList? _TagList, TimeSpan? _RecurranceTimeSpan, bool _garbled, TaskPriority _taskPriority, ArrayList? _Attachments = null)
        {
            InitializeComponent();

            UID = _TaskUID;

            taskNameTextBlock.Text = Name = _TaskName;
            TaskToolTipLabel.Content = (_TaskName.Length > 100) ? ($"{_TaskName[..100]}...") : _TaskName;

            if ((Desc = _TaskDesc) != "") DescriptionIcon.Visibility = Visibility.Visible;

            CreatedDT = _CreatedDT;
            DueDT = _DueDT;
            IsCompleted = _CompletedDT.HasValue;
            CompletedDT = _CompletedDT;

            taskStatus = _taskStatus;
            if ((RecurranceTimeSpan = _RecurranceTimeSpan) is not null) RepeatIcon.Visibility = Visibility.Visible;
            TagList = _TagList;
            Attachments = _Attachments;

            Garbled = _garbled;
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
            if (e.ChangedButton == MouseButton.Left)
                if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
                    ToggleCompletionStatus();
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

            if (IsCompleted && RecurranceTimeSpan.HasValue)
            {
                long randomLong;
            randomGen:
                randomLong = (long)(new Random().NextDouble() * long.MaxValue);
                foreach (IndividualTask task in TaskFile.TaskList) if (task.UID == randomLong) { goto randomGen; }

                DateTime? newDateTime = (DueDT.HasValue) ?
                    DueDT.Value + RecurranceTimeSpan.Value :
                    DateTimeOffset.UtcNow.LocalDateTime + RecurranceTimeSpan.Value;

                TaskFile.TaskList.Add(new IndividualTask(randomLong, Name, Desc, DateTimeOffset.UtcNow.LocalDateTime, newDateTime, null, IndividualTask.TaskStatus.Normal, TagList, RecurranceTimeSpan, Garbled, taskPriority, Attachments));

                RecurranceTimeSpan = null;
            }

            TaskFile.SaveData();
            NewDueDT();
            UpdateTaskCheckBoxAndBackground();
        }

        public void WontDoTask()
        {
            if (taskStatus == TaskStatus.WontDo)
            {
                taskStatus = TaskStatus.Normal;
                taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                NewDueDT();
            }
            else
            {
                taskStatus = TaskStatus.WontDo;
                taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0.25, TimeSpan.FromMilliseconds(250)));
                TaskTimer.Stop();
            }
            TaskFile.SaveData();
        }

        public void UpdateTaskCheckBoxAndBackground()
        {
            checkMark.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted ? (taskPriority == TaskPriority.High ? 0.75 : 1) : 0, TimeSpan.FromMilliseconds(250)));
            taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted ? 0.25 : 1, TimeSpan.FromMilliseconds(250)));
            UpdateHP();
            UpdateStrikethrough();
            DueDateTimeLabelUpdate();
        }

        private void UpdateHP()
        {
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
            taskPriority = (taskPriority == TaskPriority.Normal) ? TaskPriority.High : TaskPriority.Normal;
            TaskFile.SaveData();
            UpdateHP();
        }

        private async void UpdateStrikethrough()
        {
            await Task.Delay(250);

            DoubleAnimation animation;

            int duration = (int)(4 * strikethroughLine.MaxWidth);

            if (IsCompleted && strikethroughLine.Width != strikethroughLine.MaxWidth)
                animation = new DoubleAnimation(0, strikethroughLine.MaxWidth, TimeSpan.FromMilliseconds(duration));

            else if (!IsCompleted && strikethroughLine.Width != 0)
                animation = new DoubleAnimation(strikethroughLine.MaxWidth, 0, TimeSpan.FromMilliseconds(duration));

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
                DueDateTimeLabel.Content = $"Done {DateDifference(CompletedDT.Value)}{CompletedDT.Value:hh:mm tt}";

            else if (DueDT.HasValue)
                DueDateTimeLabel.Content = $"Due {DateDifference(DueDT.Value)}{DueDT.Value:hh:mm tt}";

            else
                DueDateTimeLabel.Content = "";

            DueDateTimeLabel.Foreground = (SolidColorBrush)mainWindow.FindResource((IsDue && !IsCompleted) ? "PastDue" : "Text");
            DueDateTimeLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(IsCompleted ? 0.25 : (IsDue ? 1 : 0.5), TimeSpan.FromMilliseconds(250)));
        }

        private string DateDifference(DateTime dateTime)
        {
            return (dateTime.Year != DateTime.Now.Year) ? dateTime.ToString("dd\\/MM\\/yyyy ") : (dateTime.Date - DateTime.Now.Date).Days switch
            {
                0 => "",
                -1 => "yesterday ",
                1 => "tomorrow ",
                _ => dateTime.ToString("dd\\/MM ")
            };
        }

        private void UpdateDateTimeLabelWithRemaining(object sender, EventArgs e)
        {
            if (!DueDT.HasValue) return;
            TimeSpan remainingTime = DueDT.Value - DateTime.Now;
            if (remainingTime <= TimeSpan.FromMinutes(1))
            {
                if (remainingTime <= TimeSpan.FromTicks(0))
                {
                    if (!IsCompleted) DueDateTimeLabel.Content = "Past due";
                    TemporaryRemainingTimer.Stop();
                }
                else DueDateTimeLabel.Content = $"In {(int)remainingTime.TotalSeconds} seconds";
            }
            else
            {
                DueDateTimeLabel.Content = $"In {DTHelper.GetRemainingDueTime(remainingTime)}";
                TemporaryRemainingTimer.Stop();
            }
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
            if (taskStatus == TaskStatus.Normal && DueDT.HasValue && !IsCompleted)
            {
                double taskTimeRemaining = (DueDT.Value - DateTime.Now).TotalSeconds;
                if (taskTimeRemaining <= 120)
                {
                    TaskTimer.Interval = TimeSpan.FromSeconds(Math.Max(2, taskTimeRemaining));
                    TaskTimer.Tick += (s, e) =>
                    {
                        DueDateTimeLabel.Foreground = (SolidColorBrush)mainWindow.FindResource("PastDue");
                        DueDateTimeLabel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(250)));
                        mainWindow.OnTaskDue(this);
                        //if (TaskFile.notificationMode == TaskFile.NotificationMode.Normal)
                        //    mainWindow.OnTaskDue(garbled ? "Garbled Task" : TaskName, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                        //else if (TaskFile.notificationMode == TaskFile.NotificationMode.High && taskPriority == TaskPriority.High)
                        //    mainWindow.OnTaskDue(garbled ? "Garbled Task" : TaskName, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
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
                    "plus1m" => TimeSpan.FromMinutes(1),
                    "plus5m" => TimeSpan.FromMinutes(5),
                    "plus10m" => TimeSpan.FromMinutes(10),
                    "plus30m" => TimeSpan.FromMinutes(30),
                    "plus1h" => TimeSpan.FromHours(1),
                    "plus6h" => TimeSpan.FromHours(6),
                    "plus12h" => TimeSpan.FromHours(12),
                    "plus1d" => TimeSpan.FromDays(1),
                    "plus1w" => TimeSpan.FromDays(7),
                    _ => TimeSpan.FromTicks(0),
                };

            TaskFile.SaveData();
            DueDateTimeLabelUpdate();
            NewDueDT();
        }

        public new bool IsVisible = true;

        public void Appear(int timeSpan = 250)
        {
            Visibility = Visibility.Visible;
            ChangeHeight(0, 60, timeSpan);
            IsVisible = true;
        }

        public async void Disappear()
        {
            ChangeHeight(ActualHeight, 0);
            IsVisible = false;
            await Task.Delay(250);
            Visibility = Visibility.Collapsed;
        }

        private void ChangeHeight(double oldValue, double newValue, int timeSpan = 250)
        {
            var animation = new DoubleAnimation(oldValue, newValue, TimeSpan.FromMilliseconds(timeSpan));
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));

            Storyboard storyboard = new();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        public bool IsLinkAvailable()
        {
            return LinkRegex().Match(Name).Success;
        }

        public void LinkOpen()
        {
            Match match = LinkRegex().Match(Name);
            if (match.Success)
            {
                Process.Start(new ProcessStartInfo("cmd", "/C start" + " " + match.Value));
            }
        }

        [GeneratedRegex("\\b(https://|www.)\\S+")]
        public static partial Regex LinkRegex();

        public bool IsGarbled()
        {
            return Garbled;
        }

        public async void Garble(bool? _garble = null, bool playAnimation = false)
        {
            bool garble = _garble ?? !IsGarbled();

            if (garble == Garbled) return;
            Garbled = garble;

            if (IsVisible && playAnimation)
            {
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

            else TempGarble(garble ? TempGarbleMode.On : TempGarbleMode.Off);

            TaskFile.SaveData();
        }

        public async void TempGarble(TempGarbleMode garbleMode, bool playAnimation = false)
        {
            if ((garbleMode == TempGarbleMode.On || (garbleMode == TempGarbleMode.None && Garbled)) && TextSP.Opacity != 1)
            {
                if (playAnimation)
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
            }
            else if ((garbleMode == TempGarbleMode.Off || (garbleMode == TempGarbleMode.None && !Garbled)) && TextSP.Opacity != 0)
            {
                if (playAnimation)
                {
                    TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
                    await Task.Delay(400);
                    TextSP.Children.Clear();

                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromTicks(0)));
                    taskNameTextBlock.Visibility = Visibility.Visible;
                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, IsCompleted ? 0.25 : 1, TimeSpan.FromMilliseconds(300)));

                    strikethroughLine.Visibility = Visibility.Visible;
                }

                else
                {
                    TextSP.Children.Clear();

                    strikethroughLine.Visibility = Visibility.Visible;
                    taskNameTextBlock.Visibility = Visibility.Visible;
                    taskNameTextBlock.BeginAnimation(OpacityProperty, new DoubleAnimation(0, IsCompleted ? 0.25 : 1, TimeSpan.FromTicks(0)));

                    TextSP.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromTicks(0)));
                }
            }
        }
    }
}
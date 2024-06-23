using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TemporaTasks.Pages;
using TemporaTasks.UserControls;
using static TemporaTasks.UserControls.IndividualTask;

namespace TemporaTasks.Core
{
    public static class TaskFile
    {
        public static string saveFilePath = null;
        public static string backupPath;

        public static ArrayList TaskList;

        public static Dictionary<string, Dictionary<string, string>> globalData = [];

        public static int sortType = 2;

        public enum NotificationMode
        {
            Normal = 0,
            High = 1,
            Muted = 2
        }

        public static NotificationMode notificationMode = NotificationMode.Normal;
        public static bool notifPopupMode = false;
        public static IndividualTask.TempGarbleMode tempGarbleMode = IndividualTask.TempGarbleMode.None;

        public static DispatcherTimer NotificationModeTimer = new();
        public static DateTime NotificationModeTimerStart;

        public async static void LoadData()
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Cursor = Cursors.Wait;

            TaskList = [];

            if (File.Exists(saveFilePath))
            {
                globalData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));
                
                HomePage.initialFinished = false;

                if (globalData.Keys.Contains("settings"))
                {
                    Dictionary<string, string> settings = globalData["settings"];
                    globalData.Remove("settings");

                    sortType = int.Parse(settings["sortType"]);
                    notificationMode = (NotificationMode)Enum.Parse(typeof(NotificationMode), settings["notifMode"]);
                    notifPopupMode = settings["notifPopupMode"] != "0";
                }

                // double increment = 1/data.Count;
                // double scale = 0;

                foreach (string taskUID in globalData.Keys)
                {
                    try
                    {
                        IndividualTask.TaskStatus taskStatus = (IndividualTask.TaskStatus)Enum.Parse(typeof(IndividualTask.TaskStatus), globalData[taskUID]["taskStatus"]);

                        if (taskStatus != IndividualTask.TaskStatus.Completed)
                        {
                            DateTime? createdTime = null;
                            createdTime = StringToDateTime(globalData, taskUID, "createdTime");

                            DateTime? dueTime = null;
                            dueTime = StringToDateTime(globalData, taskUID, "dueTime");

                            DateTime? completedTime = null;
                            completedTime = StringToDateTime(globalData, taskUID, "completedTime");

                            TimeSpan? recurrance = null;
                            if (globalData[taskUID].TryGetValue("recurrance", out string? recurranceString) && recurranceString != "") recurrance = TimeSpan.Parse(recurranceString);

                            ArrayList? tagList = null;
                            if (globalData[taskUID]["tags"] != "") tagList = new ArrayList(globalData[taskUID]["tags"].Split(';'));

                            ArrayList? attachments = null;
                            // if (globalData[taskUID]["attachments"] != "") attachments = new ArrayList(globalData[taskUID]["attachments"].Split(';'));

                            bool garbled = false;
                            if (globalData[taskUID]["garbled"] != "0") garbled = true;

                            IndividualTask.TaskPriority taskPriority = (IndividualTask.TaskPriority)Enum.Parse(typeof(IndividualTask.TaskPriority), globalData[taskUID]["taskPriority"]);

                            IndividualTask taskObj = new(long.Parse(taskUID), globalData[taskUID]["taskName"], globalData[taskUID]["taskDesc"], createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, attachments);
                            TaskList.Add(taskObj);
                        }
                    }
                    catch { }
                    //await Task.Delay(1);
                    // mainWindow.LoadBar.RenderTransform = new ScaleTransform(scale += increment, 1);
                }

                mainWindow.WindowTitle.Content = $" | {Path.GetFileName(saveFilePath)}";
            }

            mainWindow.Cursor = Cursors.Arrow;
            mainWindow.LoadPage();
        }

        public static void OpenTasksFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "TemporaTask File (*.ttask)|*.ttask";
            openFileDialog.InitialDirectory = Path.GetDirectoryName(saveFilePath);

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string oldPath = saveFilePath;
                try
                {
                    saveFilePath = selectedFilePath;
                    LoadData();
                }
                catch
                {
                    saveFilePath = oldPath;
                    LoadData();
                }
            }
        }

        public static void SaveTasksFile()
        {
            SaveFileDialog saveFileDialog = new();
            saveFileDialog.Filter = "TemporaTask File (*.ttask)|*.ttask";
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(saveFilePath);
            saveFileDialog.FileName = Path.GetFileName(saveFilePath);

            if (saveFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = saveFileDialog.FileName;
                string oldPath = saveFilePath;
                try
                {
                    saveFilePath = selectedFilePath;
                    SaveData(force: true);
                }
                catch
                {
                    saveFilePath = oldPath;
                    SaveData(force: true);
                }
            }
        }

        public static void ImportTasks()
        {
            try
            {
                string clipText = Clipboard.GetText();
                if (clipText == null) return;

                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(clipText);

                if (data.Keys.Contains("settings")) data.Remove("settings");

                foreach (string taskUID in data.Keys)
                {
                    try
                    {
                        string? taskDesc = null;
                        if (data[taskUID].TryGetValue("taskDesc", out string? value)) taskDesc = value;

                        DateTime? createdTime = null;
                        if (data[taskUID].ContainsKey("createdTime")) createdTime = StringToDateTime(data, taskUID, "createdTime");

                        DateTime? dueTime = null;
                        if (data[taskUID].ContainsKey("dueTime")) dueTime = StringToDateTime(data, taskUID, "dueTime");

                        DateTime? completedTime = null;
                        if (data[taskUID].ContainsKey("completedTime")) completedTime = StringToDateTime(data, taskUID, "completedTime");

                        IndividualTask.TaskStatus taskStatus = IndividualTask.TaskStatus.Normal;
                        if (data[taskUID].TryGetValue("taskStatus", out string? taskStatusString)) taskStatus = (IndividualTask.TaskStatus)Enum.Parse(typeof(IndividualTask.TaskStatus), taskStatusString);

                        ArrayList? tagList = null;
                        if (data[taskUID].TryGetValue("tags", out string? tagString) && tagString != "") tagList = new ArrayList(tagString.Split(';'));

                        TimeSpan? recurrance = null;
                        if (data[taskUID].TryGetValue("recurrance", out string? recurranceString) && recurranceString != "") recurrance = TimeSpan.Parse(recurranceString);

                        bool garbled = false;
                        if (data[taskUID].TryGetValue("garbled", out string? garbledString) && garbledString != "0") garbled = true;

                        TaskPriority taskPriority = TaskPriority.Normal;
                        if (data[taskUID].TryGetValue("taskPriority", out string? priorityText)) taskPriority = (TaskPriority)Enum.Parse(typeof(TaskPriority), priorityText);

                        IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], taskDesc, createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, null);
                        TaskList.Add(taskObj);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static DateTime? StringToDateTime(Dictionary<string, Dictionary<string, string>> data, string taskUID, string field)
        {
            if (data[taskUID][field] == "") return null;
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[taskUID][field])).LocalDateTime;
        }

        public static bool saveLock = false;

        public static async void SaveData(MainWindow? mainWindow = null, bool force = false)
        {
            if (mainWindow == null && !force)
            {
                if (saveLock) return;
                saveLock = true;
                await Task.Delay(5000);
                if (!saveLock) return;
            }

            Dictionary<string, Dictionary<string, string>> temp = [];

            Dictionary<string, string> temp2 = [];
            temp2["sortType"] = sortType.ToString();
            temp2["notifMode"] = ((int)notificationMode).ToString();
            temp2["notifPopupMode"] = notifPopupMode ? "1" : "0";
            globalData["settings"] = temp2;

            foreach (IndividualTask task in TaskList)
            {
                temp2 = [];
                temp2["taskName"] = task.Name;
                temp2["taskDesc"] = task.Desc;
                temp2["createdTime"] = DateTimeToString(task.CreatedDT);
                temp2["dueTime"] = DateTimeToString(task.DueDT);
                temp2["completedTime"] = DateTimeToString(task.CompletedDT);
                temp2["taskStatus"] = ((int)task.taskStatus).ToString();
                temp2["tags"] = (task.TagList == null) ? "" : string.Join(';', task.TagList.ToArray());
                temp2["recurrance"] = task.RecurranceTimeSpan.HasValue ? task.RecurranceTimeSpan.Value.ToString() : "";
                temp2["garbled"] = task.IsGarbled() ? "1" : "0";
                temp2["taskPriority"] = task.taskPriority == IndividualTask.TaskPriority.High ? "1" : "0";
                temp2["attachments"] = (task.Attachments == null) ? "" : string.Join(';', task.Attachments.ToArray());
                globalData[task.UID.ToString()] = temp2;
            }

            string temp3 = JsonSerializer.Serialize(globalData);
            File.WriteAllText(saveFilePath, temp3);

            string saveTime = DateTime.Now.ToString("yyMMddHHmm");
            File.WriteAllText($"{backupPath}\\data{saveTime}.json", temp3);

            if (mainWindow != null) mainWindow.Close();

            saveLock = false;
        }

        public static string DateTimeToString(DateTime? dateTime)
        {
            if (dateTime.HasValue) return ((long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            else return "";
        }
    }
}
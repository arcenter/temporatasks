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

        public static int sortType = 3;

        public enum NotificationMode
        {
            Normal = 0,
            High = 1,
            Muted = 2
        }

        public static NotificationMode notificationMode = NotificationMode.Normal;
        public static bool popupOnNotification = true;
        public static IndividualTask.TempGarbleMode tempGarbleMode = IndividualTask.TempGarbleMode.None;

        public static DispatcherTimer muteNotificationsTimer = new();
        public static DateTime muteNotificationsTimerEnd;

        public async static void LoadData()
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.Cursor = Cursors.Wait;

            TaskList = [];

            if (File.Exists(saveFilePath))
            {
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));

                HomePage.initialFinished = false;

                Dictionary<string, string> settings = data["settings"];
                data.Remove("settings");

                sortType = int.Parse(settings["sortType"]);
                notificationMode = (NotificationMode)Enum.Parse(typeof(NotificationMode), settings["notificationMode"]);
                if (settings["muteTimerEnd"] != "0")
                {
                    var timer = DateTimeOffset.FromUnixTimeSeconds(long.Parse(settings["muteTimerEnd"])).LocalDateTime;
                    var remaining = timer - DateTime.Now;
                    if ((remaining) > TimeSpan.Zero)
                    {
                        notificationMode = NotificationMode.Muted;
                        muteNotificationsTimerEnd = timer;
                        muteNotificationsTimer.Interval = remaining;
                        muteNotificationsTimer.Start();
                    }
                    else
                    {
                        notificationMode = NotificationMode.Normal;
                    }
                }
                popupOnNotification = settings["popupOnNotification"] == "1";

                foreach (string taskUID in data.Keys)
                {
                    try
                    {
                        IndividualTask.TaskStatus taskStatus = (IndividualTask.TaskStatus)Enum.Parse(typeof(IndividualTask.TaskStatus), data[taskUID]["status"]);

                        {
                            DateTime? createdTime = null;
                            createdTime = StringToDateTime(data, taskUID, "created");

                            DateTime? modifiedTime = createdTime;
                            if (data[taskUID].ContainsKey("modified")) modifiedTime = StringToDateTime(data, taskUID, "modified");

                            DateTime? dueTime = null;
                            dueTime = StringToDateTime(data, taskUID, "due");

                            DateTime? completedTime = null;
                            completedTime = StringToDateTime(data, taskUID, "completed");

                            TimeSpan? recurrance = null;
                            if (data[taskUID].TryGetValue("recurrance", out string? recurranceString) && recurranceString != "") recurrance = TimeSpan.Parse(recurranceString);

                            ArrayList? tagList = null;
                            if (data[taskUID]["tags"] != "") tagList = new ArrayList(data[taskUID]["tags"].Split(';'));

                            ArrayList? attachments = null;
                            // if (globalData[taskUID]["attachments"] != "") attachments = new ArrayList(globalData[taskUID]["attachments"].Split(';'));

                            bool garbled = false;
                            if (data[taskUID]["garbled"] != "0") garbled = true;

                            IndividualTask.TaskPriority taskPriority = (IndividualTask.TaskPriority)Enum.Parse(typeof(IndividualTask.TaskPriority), data[taskUID]["priority"]);

                            IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["name"], data[taskUID]["desc"], createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, attachments) { modifiedDT = modifiedTime };
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
                        long newTaskUID = long.Parse(taskUID);
                        foreach (IndividualTask task in TaskList)
                            if (task.UID == newTaskUID)
                            {
                                newTaskUID = task.UID;
                                break;
                            }

                        if (!(data[taskUID].TryGetValue("name", out string? taskName))) continue;

                        data[taskUID].TryGetValue("desc", out string? taskDesc);
                        if (taskDesc is null) taskDesc = "";

                        DateTime? createdTime = null;
                        if (data[taskUID].ContainsKey("created")) createdTime = StringToDateTime(data, taskUID, "created");

                        DateTime? modifiedTime = null;
                        if (data[taskUID].ContainsKey("modified")) modifiedTime = StringToDateTime(data, taskUID, "modified");

                        DateTime? dueTime = null;
                        if (data[taskUID].ContainsKey("due")) dueTime = StringToDateTime(data, taskUID, "due");

                        DateTime? completedTime = null;
                        if (data[taskUID].ContainsKey("completed")) completedTime = StringToDateTime(data, taskUID, "completed");

                        IndividualTask.TaskStatus taskStatus = IndividualTask.TaskStatus.Normal;
                        if (data[taskUID].TryGetValue("status", out string? taskStatusString)) taskStatus = (IndividualTask.TaskStatus)Enum.Parse(typeof(IndividualTask.TaskStatus), taskStatusString);

                        ArrayList? tagList = null;
                        if (data[taskUID].TryGetValue("tags", out string? tagString) && tagString != "") tagList = new ArrayList(tagString.Split(';'));

                        TimeSpan? recurrance = null;
                        if (data[taskUID].TryGetValue("recurrance", out string? recurranceString) && recurranceString != "") recurrance = TimeSpan.Parse(recurranceString);

                        bool garbled = false;
                        if (data[taskUID].TryGetValue("garbled", out string? garbledString) && garbledString != "0") garbled = true;

                        TaskPriority taskPriority = TaskPriority.Normal;
                        if (data[taskUID].TryGetValue("priority", out string? priorityText)) taskPriority = (TaskPriority)Enum.Parse(typeof(TaskPriority), priorityText);

                        IndividualTask taskObj = new(newTaskUID, taskName, taskDesc, createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, null);
                        taskObj.modifiedDT = modifiedTime;

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

            string data = JsonSerializer.Serialize(GetTemporaDataDict());
            File.WriteAllText(saveFilePath, data);

            string saveTime = DateTime.Now.ToString("yyMMddHHmm");
            File.WriteAllText($"{backupPath}\\{Path.GetFileNameWithoutExtension(saveFilePath)}-{saveTime}.ttask", data);

            mainWindow?.Close();

            saveLock = false;
        }

        public static Dictionary<string, Dictionary<string, string>> GetTemporaDataDict()
        {
            Dictionary<string, Dictionary<string, string>> data = [];
            Dictionary<string, string> temp = [];
            
            temp["sortType"] = sortType.ToString();
            temp["notificationMode"] = ((int)notificationMode).ToString();
            temp["muteTimerEnd"] = muteNotificationsTimer.IsEnabled ? DateTimeToString(muteNotificationsTimerEnd) : "0";
            temp["popupOnNotification"] = popupOnNotification ? "1" : "0";
            data["settings"] = temp;

            foreach (IndividualTask task in TaskList)
            {
                temp = [];
                temp["name"] = task.name;
                temp["desc"] = task.desc;
                temp["created"] = DateTimeToString(task.createdDT);
                temp["modified"] = DateTimeToString(task.modifiedDT);
                temp["due"] = DateTimeToString(task.dueDT);
                temp["completed"] = DateTimeToString(task.completedDT);
                temp["status"] = ((int)task.status).ToString();
                temp["tags"] = (task.tagList == null) ? "" : string.Join(';', task.tagList.ToArray());
                temp["recurrance"] = task.recurranceTS.HasValue ? task.recurranceTS.Value.ToString() : "";
                temp["garbled"] = task.IsGarbled() ? "1" : "0";
                temp["priority"] = task.priority == TaskPriority.High ? "1" : "0";
                temp["attachments"] = (task.attachments == null) ? "" : string.Join(';', task.attachments.ToArray());
                data[task.UID.ToString()] = temp;
            }

            return data;
        }

        public static string DateTimeToString(DateTime? dateTime)
        {
            if (dateTime.HasValue) return ((long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            else return "";
        }
    }
}
using Microsoft.Win32;
using System.Collections;
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
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));
                
                HomePage.initialFinished = false;

                if (data.ContainsKey("settings"))
                {
                    Dictionary<string, string> settings = data["settings"];
                    data.Remove("settings");

                    sortType = int.Parse(settings["sortType"]);
                    notificationMode = (NotificationMode)Enum.Parse(typeof(NotificationMode), settings["notifMode"]);
                    notifPopupMode = settings["notifPopupMode"] != "0";
                }

                foreach (string taskUID in data.Keys)
                {
                    try
                    {
                        IndividualTask.TaskStatus taskStatus = (IndividualTask.TaskStatus)Enum.Parse(typeof(IndividualTask.TaskStatus), data[taskUID]["taskStatus"]);

                        {
                            DateTime? createdTime = null;
                            createdTime = StringToDateTime(data, taskUID, "createdTime");

                            DateTime? modifiedTime = createdTime;
                            if (data[taskUID].ContainsKey("modifiedTime")) modifiedTime = StringToDateTime(data, taskUID, "modifiedTime");

                            DateTime? dueTime = null;
                            dueTime = StringToDateTime(data, taskUID, "dueTime");

                            DateTime? completedTime = null;
                            completedTime = StringToDateTime(data, taskUID, "completedTime");

                            TimeSpan? recurrance = null;
                            if (data[taskUID].TryGetValue("recurrance", out string? recurranceString) && recurranceString != "") recurrance = TimeSpan.Parse(recurranceString);

                            ArrayList? tagList = null;
                            if (data[taskUID]["tags"] != "") tagList = new ArrayList(data[taskUID]["tags"].Split(';'));

                            ArrayList? attachments = null;
                            // if (globalData[taskUID]["attachments"] != "") attachments = new ArrayList(globalData[taskUID]["attachments"].Split(';'));

                            bool garbled = false;
                            if (data[taskUID]["garbled"] != "0") garbled = true;

                            IndividualTask.TaskPriority taskPriority = (IndividualTask.TaskPriority)Enum.Parse(typeof(IndividualTask.TaskPriority), data[taskUID]["taskPriority"]);

                            IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], data[taskUID]["taskDesc"], createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, attachments) { ModifiedDT = modifiedTime };
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

                        if (!(data[taskUID].TryGetValue("taskName", out string? taskName))) continue;

                        data[taskUID].TryGetValue("taskDesc", out string? taskDesc);
                        if (taskDesc is null) taskDesc = "";

                        DateTime? createdTime = null;
                        if (data[taskUID].ContainsKey("createdTime")) createdTime = StringToDateTime(data, taskUID, "createdTime");

                        DateTime? modifiedTime = null;
                        if (data[taskUID].ContainsKey("modifiedTime")) modifiedTime = StringToDateTime(data, taskUID, "modifiedTime");

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

                        IndividualTask taskObj = new(newTaskUID, taskName, taskDesc, createdTime, dueTime, completedTime, taskStatus, tagList, recurrance, garbled, taskPriority, null);
                        taskObj.ModifiedDT = modifiedTime;

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
            temp["notifMode"] = (NotificationModeTimer.IsEnabled) ? "0" : ((int)notificationMode).ToString();
            temp["notifPopupMode"] = notifPopupMode ? "1" : "0";
            data["settings"] = temp;

            foreach (IndividualTask task in TaskList)
            {
                temp = [];
                temp["taskName"] = task.Name;
                temp["taskDesc"] = task.Desc;
                temp["createdTime"] = DateTimeToString(task.CreatedDT);
                temp["modifiedTime"] = DateTimeToString(task.ModifiedDT);
                temp["dueTime"] = DateTimeToString(task.DueDT);
                temp["completedTime"] = DateTimeToString(task.CompletedDT);
                temp["taskStatus"] = ((int)task.taskStatus).ToString();
                temp["tags"] = (task.TagList == null) ? "" : string.Join(';', task.TagList.ToArray());
                temp["recurrance"] = task.RecurranceTimeSpan.HasValue ? task.RecurranceTimeSpan.Value.ToString() : "";
                temp["garbled"] = task.IsGarbled() ? "1" : "0";
                temp["taskPriority"] = task.taskPriority == IndividualTask.TaskPriority.High ? "1" : "0";
                temp["attachments"] = (task.Attachments == null) ? "" : string.Join(';', task.Attachments.ToArray());
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
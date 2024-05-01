using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using TemporaTasks.UserControls;

namespace TemporaTasks.Core
{
    public static class TaskFile
    {
        public static string saveFilePath;
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
        public static IndividualTask.TempGarbleMode tempGarbleMode = IndividualTask.TempGarbleMode.None;
        
        public static DispatcherTimer NotificationModeTimer = new();

        public static void LoadData()
        {
            ArrayList _TasksList = [];
            if (File.Exists(saveFilePath))
            {
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));

                if (data.Keys.Contains("settings"))
                {
                    Dictionary<string, string> settings = data["settings"];
                    data.Remove("settings");

                    sortType = int.Parse(settings["sortType"]);
                    notificationMode = (NotificationMode)Enum.Parse(typeof(NotificationMode), settings["notifMode"]);
                }

                foreach (string taskUID in data.Keys)
                {
                    try
                    {
                        DateTime? createdTime = null;
                        createdTime = StringToDateTime(data, taskUID, "createdTime");

                        DateTime? dueTime = null;
                        dueTime = StringToDateTime(data, taskUID, "dueTime");

                        DateTime? completedTime = null;
                        completedTime = StringToDateTime(data, taskUID, "completedTime");

                        // TimeSpan? recurranceTimeSpan = null;
                        // if (data[taskUID]["recurranceTS"] != "") recurranceTimeSpan = TimeSpan.Parse(data[taskUID]["recurranceTS"]);

                        ArrayList? tagList = null;
                        if (data[taskUID]["tags"] != "") tagList = new ArrayList(data[taskUID]["tags"].Split(';'));

                        ArrayList? attachments = null;
                        // if (data[taskUID]["attachments"] != "") attachments = new ArrayList(data[taskUID]["attachments"].Split(';'));

                        bool garbled = false;
                        if (data[taskUID]["garbled"] != "0") garbled = true;

                        IndividualTask.TaskPriority taskPriority = (IndividualTask.TaskPriority)Enum.Parse(typeof(IndividualTask.TaskPriority), data[taskUID]["taskPriority"]);

                        IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], createdTime, dueTime, completedTime, tagList, null, garbled, taskPriority, attachments);
                        _TasksList.Add(taskObj);
                    }
                    catch { }
                }
            }
            TaskList = _TasksList;
        }

        private static DateTime? StringToDateTime(Dictionary<string, Dictionary<string, string>> data, string taskUID, string field)
        {
            if (data[taskUID][field] == "") return null;
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[taskUID][field])).LocalDateTime;
        }

        public static bool saveLock = false;

        public static async void SaveData(MainWindow? mainWindow = null)
        {
            if (mainWindow == null)
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
            temp["settings"] = temp2;

            foreach (IndividualTask task in TaskList)
            {
                temp2 = [];
                temp2["taskName"] = task.TaskName;
                temp2["createdTime"] = DateTimeToString(task.CreatedDT);
                temp2["dueTime"] = DateTimeToString(task.DueDT);
                temp2["completedTime"] = DateTimeToString(task.CompletedDT);
                // temp2["recurranceTS"] = task.RecurranceTimeSpan.HasValue ? task.RecurranceTimeSpan.ToString() : "";
                temp2["tags"] = (task.TagList == null) ? "" : string.Join(';', task.TagList.ToArray());
                temp2["garbled"] = task.IsGarbled() ? "1" : "0";
                temp2["taskPriority"] = task.taskPriority == IndividualTask.TaskPriority.High ? "1" : "0";
                temp2["attachments"] = (task.Attachments == null) ? "" : string.Join(';', task.Attachments.ToArray());
                temp[task.TaskUID.ToString()] = temp2;
            }
            string temp3 = JsonSerializer.Serialize(temp);
            File.WriteAllText(saveFilePath, temp3);

            string saveTime = DateTime.Now.ToString("yyMMddHHmm");
            File.WriteAllText($"{backupPath}\\data{saveTime}.json", temp3);

            if (mainWindow != null) mainWindow.Close();

            saveLock = false;
        }

        private static string DateTimeToString(DateTime? dateTime)
        {
            if (dateTime.HasValue) return ((long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            else return "";
        }
    }
}
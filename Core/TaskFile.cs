﻿using System;
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TemporaTasks.UserControls;
using static TemporaTasks.UserControls.IndividualTask;

namespace TemporaTasks.Core
{
    public static class TaskFile
    {
        public static string saveFilePath = null;
        public static string backupPath;
        
        public static ArrayList TaskList = [];

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

            if (File.Exists(saveFilePath))
            {
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));

                if (data.Keys.Contains("settings"))
                {
                    Dictionary<string, string> settings = data["settings"];
                    data.Remove("settings");

                    sortType = int.Parse(settings["sortType"]);
                    notificationMode = (NotificationMode)Enum.Parse(typeof(NotificationMode), settings["notifMode"]);
                    notifPopupMode = settings["notifPopupMode"] != "0";
                }

                // double increment = 1/data.Count;
                // double scale = 0;
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

                        IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], data[taskUID]["taskDesc"], createdTime, dueTime, completedTime, tagList, null, garbled, taskPriority, attachments);
                        TaskList.Add(taskObj);
                    }
                    catch { }
                    await Task.Delay(1);
                    // mainWindow.LoadBar.RenderTransform = new ScaleTransform(scale += increment, 1);
                }
            }

            mainWindow.Cursor = Cursors.Arrow;
            mainWindow.LoadPage();
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
                        DateTime? createdTime = null;
                        if (data[taskUID].ContainsKey("createdTime")) createdTime = StringToDateTime(data, taskUID, "createdTime");

                        DateTime? dueTime = null;
                        if (data[taskUID].ContainsKey("dueTime")) dueTime = StringToDateTime(data, taskUID, "dueTime");

                        DateTime? completedTime = null;
                        if (data[taskUID].ContainsKey("completedTime")) completedTime = StringToDateTime(data, taskUID, "completedTime");

                        ArrayList? tagList = null;
                        if (data[taskUID].ContainsKey("tags") && data[taskUID]["tags"] != "") tagList = new ArrayList(data[taskUID]["tags"].Split(';'));

                        bool garbled = false;
                        if (data[taskUID].TryGetValue("garbled", out string? value) && value != "0") garbled = true;

                        TaskPriority taskPriority = TaskPriority.Normal;
                        if (data[taskUID].TryGetValue("taskPriority", out string? value2)) taskPriority = (TaskPriority)Enum.Parse(typeof(TaskPriority), value2);

                        IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], data[taskUID]["taskDesc"], createdTime, dueTime, completedTime, tagList, null, garbled, taskPriority, null);
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
            temp2["notifPopupMode"] = notifPopupMode ? "1" : "0";
            temp["settings"] = temp2;

            foreach (IndividualTask task in TaskList)
            {
                temp2 = [];
                temp2["taskName"] = task.TaskName;
                temp2["taskDesc"] = task.TaskDesc;
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

        public static string DateTimeToString(DateTime? dateTime)
        {
            if (dateTime.HasValue) return ((long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            else return "";
        }
    }
}
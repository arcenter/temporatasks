using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using TemporaTasks.UserControls;

namespace TemporaTasks.Core
{
    public static class TaskFile
    {
        public static string saveFilePath;
        public static string backupPath;
        public static ArrayList TaskList;

        public static void LoadData()
        {
            ArrayList _TasksList = [];
            if (File.Exists(saveFilePath))
            {
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));
                foreach (string taskName in data.Keys)
                {
                    try
                    {
                        Nullable<DateTime> createdTime = null;
                        Nullable<DateTime> dueTime = null;
                        Nullable<DateTime> completedTime = null;

                        createdTime = StringToDateTime(data, taskName, "createdTime");
                        dueTime = StringToDateTime(data, taskName, "dueTime");
                        completedTime = StringToDateTime(data, taskName, "completedTime");

                        IndividualTask taskObj = new(taskName, createdTime, dueTime, completedTime);
                        _TasksList.Add(taskObj);
                    }
                    catch { }
                }
            }
            TaskList = _TasksList;
        }

        private static Nullable<DateTime> StringToDateTime(Dictionary<string, Dictionary<string, string>> data, string taskName, string field)
        {
            if ((string)data[taskName][field] == "") return null;
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[taskName][field])).LocalDateTime;
        }

        public static void SaveData()
        {
            Dictionary<string, Dictionary<string, string>> temp = [];
            foreach (IndividualTask task in TaskList)
            {
                Dictionary<string, string> temp2 = [];
                temp2["createdTime"] = DateTimeToString(task.CreatedDT);
                temp2["dueTime"] = DateTimeToString(task.DueDT);
                temp2["completedTime"] = DateTimeToString(task.CompletedDT);
                temp[task.TaskName] = temp2;
            }
            string temp3 = JsonSerializer.Serialize<Dictionary<string, Dictionary<string, string>>>(temp);
            File.WriteAllText(saveFilePath, temp3);

            string saveTime = DateTime.Now.ToString("yyMMddHHmm");
            File.WriteAllText($"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TemporaTasks\\backups\\data{saveTime}.json", temp3);
            temp3 = "";
        }

        private static string DateTimeToString(Nullable<DateTime> dateTime)
        {
            if (dateTime.HasValue) return ((long)(dateTime.Value.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds).ToString();
            else return "";
        }
    }
}
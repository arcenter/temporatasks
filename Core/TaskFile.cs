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

using TemporaTasks.UserControls;

namespace TemporaTasks.Core
{
    public static class TaskFile
    {
        public static string saveFilePath;
        public static string backupPath;
        
        public static ArrayList TaskList;

        public static int sortType = 1;
        
        public static void LoadData()
        {
            ArrayList _TasksList = [];
            if (File.Exists(saveFilePath))
            {
                Dictionary<string, Dictionary<string, string>> data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(saveFilePath));

                if (data.Keys.Contains("settings"))
                {
                    Dictionary<string, string>  settings = data["settings"];
                    data.Remove("settings");

                    sortType = int.Parse(settings["sortType"]);
                }

                foreach (string taskUID in data.Keys)
                {
                    try
                    {
                        Nullable<DateTime> createdTime = null;
                        Nullable<DateTime> dueTime = null;
                        Nullable<DateTime> completedTime = null;
                        ArrayList? tagList = null;

                        createdTime = StringToDateTime(data, taskUID, "createdTime");
                        dueTime = StringToDateTime(data, taskUID, "dueTime");
                        completedTime = StringToDateTime(data, taskUID, "completedTime");

                        if (data[taskUID]["tags"] != "")
                            tagList = new ArrayList(data[taskUID]["tags"].Split(';'));

                        IndividualTask taskObj = new(long.Parse(taskUID), data[taskUID]["taskName"], createdTime, dueTime, completedTime, tagList);
                        _TasksList.Add(taskObj);
                    }
                    catch { }
                }
            }
            TaskList = _TasksList;
        }

        private static Nullable<DateTime> StringToDateTime(Dictionary<string, Dictionary<string, string>> data, string taskUID, string field)
        {
            if ((string)data[taskUID][field] == "") return null;
            return DateTimeOffset.FromUnixTimeSeconds(long.Parse(data[taskUID][field])).LocalDateTime;
        }

        public static void SaveData()
        {
            Dictionary<string, Dictionary<string, string>> temp = [];

            Dictionary<string, string> temp2 = [];
            temp2["sortType"] = sortType.ToString();
            temp["settings"] = temp2;            

            foreach (IndividualTask task in TaskList)
            {
                temp2 = [];
                temp2["taskName"] = task.TaskName;
                temp2["createdTime"] = DateTimeToString(task.CreatedDT);
                temp2["dueTime"] = DateTimeToString(task.DueDT);
                temp2["completedTime"] = DateTimeToString(task.CompletedDT);
                temp2["tags"] = (task.TagList != null) ? string.Join(';', task.TagList.ToArray()) : "";
                temp[task.TaskUID.ToString()] = temp2;
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
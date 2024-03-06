using System.Collections;
using System.Text.RegularExpressions;

namespace TemporaTasks.Core
{
    public partial class DTHelper
    {
        public static string DateToString(DateTime date)
        {
            return $"{date.Year}-{date.Month.ToString().PadLeft(2, '0')}-{date.Day.ToString().PadLeft(2, '0')}";
        }

        public static string TimeToString(DateTime time)
        {
            int hour = time.Hour;
            string apm;
            if (hour < 12)
            {
                apm = "AM";
            }
            else
            {
                apm = "PM";
                hour -= 12;
            }
            return $"{hour.ToString().PadLeft(2, '0')}:{time.Minute.ToString().PadLeft(2, '0')} {apm}";
        }

        public static Nullable<DateTime> StringToDateTime(string date, string time)
        {
            if (date.Length == 0 && time.Length == 0) return null;

            int year = DateTime.Now.Year, month = DateTime.Now.Month, day = DateTime.Now.Day, hour = 0, minute = 0;
            string[] splits;

            if (date.Length != 0)
            {
                try
                {
                    Match match;

                    if ((match = RegexAddYear().Match(date)).Success)
                    {
                        DateTime newDate = DateTime.Now + TimeSpan.FromDays(365.25 * int.Parse(match.Value));
                        year = newDate.Year;
                        month = newDate.Month;
                        day = newDate.Day;
                    }
                    else if ((match = RegexAddMonth().Match(date)).Success)
                    {
                        DateTime newDate = DateTime.Now + TimeSpan.FromDays(30.5 * int.Parse(match.Value));
                        year = newDate.Year;
                        month = newDate.Month;
                        day = newDate.Day;
                    }
                    else if ((match = RegexAddDay().Match(date)).Success)
                    {
                        DateTime newDate = DateTime.Now + TimeSpan.FromDays(int.Parse(match.Value));
                        year = newDate.Year;
                        month = newDate.Month;
                        day = newDate.Day;
                    }
                    else
                    {
                        splits = date.Split('-');
                        if (RegexDateYYYYMMDD().Match(date).Success)
                        {
                            year = int.Parse(splits[0]);
                            month = int.Parse(splits[1]);
                            day = int.Parse(splits[2]);
                        }
                        else if (RegexDateMMDD().Match(date).Success)
                        {
                            month = int.Parse(splits[0]);
                            day = int.Parse(splits[1]);
                        }
                        else if (RegexDateDD().Match(date).Success)
                        {
                            day = int.Parse(splits[0]);
                        }
                        else
                        {
                            throw new IncorrectDateException();
                        }
                    }
                }
                catch
                {
                    throw new IncorrectDateException();
                }
            }

            if (time.Length != 0)
            {
                if (RegexTimeHH_MM().Match(time).Success)
                {
                    MatchCollection matches = new Regex("\\d{1,2}").Matches(time);
                    hour = int.Parse(matches[0].Value);// + (new Regex(" ?[Pp][Mm]?$").Match(time).Success ? 12 : 0);
                    minute = int.Parse(matches[1].Value);
                    if (!new Regex("[Aa]").Match(time).Success && (new DateTime(year, month, day, hour, minute - 30, 0) < DateTime.Now || new Regex(" ?[Pp][Mm]?$").Match(time).Success)) hour += 12;
                }

                else if (RegexTimeHH().Match(time).Success)
                    hour = int.Parse(new Regex("\\d{1,2}").Match(time).Value) + (new Regex(" ?[Pp][Mm]?$").Match(time).Success ? 12 : 0);

                else if (RegexTimeHHMM().Match(time).Success)
                {
                    string timeString = new Regex("\\d{3,4}").Match(time).Value.PadLeft(4, '0');
                    minute = int.Parse(timeString[2..]);
                    hour = int.Parse(timeString[..2]);
                    if (!new Regex("[Aa]").Match(time).Success && (new DateTime(year, month, day, hour, minute - 30, 0) < DateTime.Now || new Regex(" ?[Pp][Mm]?$").Match(time).Success)) hour += 12;
                }

                else
                    throw new IncorrectTimeException();

                if (hour < 0 || hour > 23 || minute < 0 || minute > 59) throw new IncorrectTimeException();
            }

            return new DateTime(year, month, day, hour, minute, 0);
        }

        public static string? matchedTime;
        public static string? matchedDate;

        public static string? RegexRelativeTimeMatch(string str)
        {
            Match match;

            match = RegexAMinute().Match(str);
            if (match.Success)
            {
                matchedTime = match.Value;
                return TimeToString(DateTime.Now.AddMinutes(1));
            }

            match = RegexMinutes().Match(str);
            if (match.Success)
            {
                matchedTime = match.Value;
                return TimeToString(DateTime.Now.AddMinutes(int.Parse(RegexDigits().Match(match.Value).Value)));
            }

            match = RegexAnHour().Match(str);
            if (match.Success)
            {
                matchedTime = match.Value; 
                return TimeToString(DateTime.Now.AddHours(1));
            }

            match = RegexHours().Match(str);
            if (match.Success)
            {
                matchedTime = match.Value;
                return TimeToString(DateTime.Now.AddHours(int.Parse(RegexDigits().Match(match.Value).Value)));
            }

            matchedTime = null;
            return null;
        }

        public static string? RegexRelativeDateMatch(string str)
        {
            Match match;

            match = new Regex("(?i)today").Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                return DateToString(DateTime.Now);
            }

            match = new Regex("(?i)day after tomorrow").Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                return DateToString(DateTime.Now.AddDays(2));
            }

            match = new Regex("(?i)tomorrow").Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                return DateToString(DateTime.Now.AddDays(1));
            }

            match = RegexDayOfWeek().Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                int currentDayOfWeek = (int)DateTime.Now.DayOfWeek;
                int selectedDayOfWeek = match.Value[3..5] switch
                {
                    "su" => 0,
                    "mo" => 1,
                    "tu" => 2,
                    "we" => 3,
                    "th" => 4,
                    "fr" => 5,
                    "sa" => 6,
                    _ => 0
                };
                if (selectedDayOfWeek <= currentDayOfWeek) selectedDayOfWeek += 7 - currentDayOfWeek;
                else selectedDayOfWeek -= currentDayOfWeek;
                return DateToString(DateTime.Now.AddDays(selectedDayOfWeek));
            }

            match = RegexDays().Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                return DateToString(DateTime.Now.AddDays(int.Parse(RegexDigits().Match(match.Value).Value)));
            }

            match = RegexMonths().Match(str);
            if (match.Success)
            {
                matchedDate = match.Value;
                return DateToString(DateTime.Now.AddMonths(int.Parse(RegexDigits().Match(match.Value).Value)));
            }

            matchedDate = null;
            return null;
        }

        public static string GetDaySuffix(int day)
        {
            return day switch
            {
                1 or 21 or 31 => "st",
                2 or 22 => "nd",
                3 or 23 => "rd",
                _ => "th",
            };
        }

        // Digits Regex --------------------------------------------------------------------

        [GeneratedRegex(@"\d{1,2}")]
        public static partial Regex RegexDigits();

        // Date Regex ----------------------------------------------------------------------

        [GeneratedRegex("^\\d{1,4}-\\d{1,2}-\\d{1,2}$")]
        public static partial Regex RegexDateYYYYMMDD();

        [GeneratedRegex("^\\+\\d{1,3}y$")]
        public static partial Regex RegexAddYear();

        [GeneratedRegex("^\\d{1,2}-\\d{1,2}$")]
        public static partial Regex RegexDateMMDD();

        [GeneratedRegex("^\\+\\d{1,3}m$")]
        public static partial Regex RegexAddMonth();

        [GeneratedRegex("^\\d{1,2}$")]
        public static partial Regex RegexDateDD();

        [GeneratedRegex("^\\+\\d{1,3}d?$")]
        public static partial Regex RegexAddDay();

        // >>> Calendar

        [GeneratedRegex(@"(?i)on .{2,5}day")]
        public static partial Regex RegexDayOfWeek();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?days?")]
        public static partial Regex RegexDays();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?months?")]
        public static partial Regex RegexMonths();

        // Time Regex ----------------------------------------------------------------------

        [GeneratedRegex("^\\d{1,2}:\\d{1,2} ?[AaPp]?[Mm]?$")]
        public static partial Regex RegexTimeHH_MM();
        
        [GeneratedRegex("^\\d{1,2} ?[AaPp]?[Mm]?$")]
        public static partial Regex RegexTimeHH();
        
        [GeneratedRegex("^\\d{3,4} ?[AaPp]?[Mm]?$")]
        public static partial Regex RegexTimeHHMM();

        // >>> Calendar

        [GeneratedRegex(@"(?i)in a min(ute)?")]
        public static partial Regex RegexAMinute();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?mins?")]
        public static partial Regex RegexMinutes();

        [GeneratedRegex(@"(?i)in an hour")]
        public static partial Regex RegexAnHour();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?hours?")]
        public static partial Regex RegexHours();

        // ---------------------------------------------------------------------------------
    }

    [Serializable]
    public class IncorrectDateException : Exception
    {
        public IncorrectDateException()
        { }
    }

    [Serializable]
    public class IncorrectTimeException : Exception
    {
        public IncorrectTimeException()
        { }
    }
}
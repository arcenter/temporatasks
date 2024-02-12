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
                hour = hour - 12;
            }
            return $"{hour.ToString().PadLeft(2, '0')}:{time.Minute.ToString().PadLeft(2, '0')} {apm}";
        }

        public static Nullable<DateTime> StringToDateTime(string date, string time)
        {
            if (date.Length == 0 && time.Length == 0) return null;

            int year = DateTime.Now.Year, month = DateTime.Now.Month, day, hour = 0, minute = 0;
            string[] splits;

            if (date.Length == 0)
            {
                year = DateTime.Now.Year;
                month = DateTime.Now.Month;
                day = DateTime.Now.Day;
            }
            else
            {
                try
                {
                    splits = date.Split('-');
                    if (new Regex("^\\d{1,4}-\\d{1,2}-\\d{1,2}$").Match(date).Success)
                    {
                        year = int.Parse(splits[0]);
                        month = int.Parse(splits[1]);
                        day = int.Parse(splits[2]);
                    }
                    else if (new Regex("^\\d{1,2}-\\d{1,2}$").Match(date).Success)
                    {
                        month = int.Parse(splits[0]);
                        day = int.Parse(splits[1]);
                    }
                    else if (new Regex("^\\d{1,2}$").Match(date).Success)
                    {
                        day = int.Parse(splits[0]);
                    }
                    else
                    {
                        throw new IncorrectDateException();
                    }
                }
                catch
                {
                    throw new IncorrectDateException();
                }
            }

            if (time.Length != 0)
            {
                if (new Regex("^\\d{1,2}:\\d{1,2} ?[AaPp]?[Mm]?$").Match(time).Success)
                {
                    MatchCollection matches = new Regex("\\d{1,2}").Matches(time);
                    hour = int.Parse(matches[0].Value) + (new Regex(" ?[Pp][Mm]?$").Match(time).Success ? 12 : 0);
                    minute = int.Parse(matches[1].Value);
                }

                else if (new Regex("^\\d{1,2} ?[AaPp]?[Mm]?$").Match(time).Success)
                    hour = int.Parse(new Regex("\\d{1,2}").Match(time).Value) + (new Regex(" ?[Pp][Mm]?$").Match(time).Success ? 12 : 0);

                else if (new Regex("^\\d{3,4} ?[AaPp]?[Mm]?$").Match(time).Success)
                {
                    string timeString = new Regex("\\d{3,4}").Match(time).Value.PadLeft(4, '0');
                    minute = int.Parse(timeString[2..]);
                    hour = int.Parse(timeString[..2]) + (new Regex(" ?[Pp][Mm]?$").Match(time).Success ? 12: 0);
                }

                else
                    throw new IncorrectTimeException();

                if (hour < 0 || hour > 23 || minute < 0 || minute > 59) throw new IncorrectTimeException();
            }

            return new DateTime(year, month, day, hour, minute, 0);
        }

        [GeneratedRegex(@"\d{1,2}", RegexOptions.None, "en-US")]
        private static partial Regex RegexDigits();

        public static string? RegexTimeMatch(string str)
        {
            Match match;

            match = RegexMinutes().Match(str);
            if (match.Success)
                return TimeToString(DateTime.Now.AddMinutes(int.Parse(RegexDigits().Match(match.Value).Value)));

            match = RegexHours().Match(str);
            if (match.Success)
                return TimeToString(DateTime.Now.AddHours(int.Parse(RegexDigits().Match(match.Value).Value)));

            return null;
        }

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?min", RegexOptions.None, "en-US")]
        private static partial Regex RegexMinutes();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?hour", RegexOptions.None, "en-US")]
        private static partial Regex RegexHours();

        public static string? RegexDateMatch(string str)
        {
            Match match;

            match = RegexDays().Match(str);
            if (match.Success)
                return DateToString(DateTime.Now.AddDays(int.Parse(RegexDigits().Match(match.Value).Value)));

            match = RegexMonths().Match(str);
            if (match.Success)
            {
                return DateToString(DateTime.Now.AddMonths(int.Parse(RegexDigits().Match(match.Value).Value)));
            }

            return null;
        }

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?day", RegexOptions.None, "en-US")]
        private static partial Regex RegexDays();

        [GeneratedRegex(@"(?i)(in|after) \d{1,2} ?month", RegexOptions.None, "en-US")]
        private static partial Regex RegexMonths();

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
        {

        }
    }
}
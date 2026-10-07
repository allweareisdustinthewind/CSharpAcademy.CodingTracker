using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.Globalization;
using System.Windows.Markup;

namespace CodingTracker;

public class CodingSession
{
   public int Id { get; set; }
   public string StartDate { get; set; }
   public string EndDate { get; set; }
   public int StartTime { get; set; }
   public int EndTime { get; set; }

   public static List <CodingSession> CreateDummySessions (DateTime dtStart, int numLogs, int maxDurationInMinutes)
   {
      List<CodingSession> sessions = new ();

      var now = DateTime.Now;
      const int maxPauseBetweenSessions = 720; // 12 hours

      Random rand = new ();
      for (int i = 0; i < numLogs; ++i)
      {
         DateTime dtEnd = dtStart.AddMinutes (rand.Next (1, maxDurationInMinutes));
         if (dtStart >= now || dtEnd >= now)
            break;

         CodingSession session = new ()
         {
            StartDate = dtStart.ToString ("yyyyMMdd"),
            EndDate   = dtEnd.ToString ("yyyyMMdd"),
            StartTime = (int) dtStart.TimeOfDay.TotalMinutes,
            EndTime   = (int) dtEnd.TimeOfDay.TotalMinutes,
         };
         sessions.Add (session);

         int pause = rand.Next (10, maxPauseBetweenSessions);
         dtStart = dtEnd.AddMinutes (pause);
      }

      return sessions;
   }

   public object GetAnonymousObject (int projectID)
   {
      return new { ProjectId = projectID, StartDate, EndDate, StartTime, EndTime};
   }

   public string GetStartTimeAsString ()
   {
      return GetStartTime ().ToString ("dd.MM.yyyy HH:mm");
   }

   public string GetEndTimeAsString ()
   {
      return GetEndTime ().ToString ("dd.MM.yyyy HH:mm");
   }

   public DateTime GetStartTime ()
   {
      return GetDateTime (StartDate, StartTime);
   }

   public DateTime GetEndTime ()
   {
      return GetDateTime (EndDate, EndTime);
   }

   public string GetDuration ()
   {
      int duration = (int)(GetDateTime (EndDate, EndTime) - GetDateTime (StartDate, StartTime)).TotalMinutes;
      return $"{duration / 60:D2}:{duration % 60:D2}";
   }

   DateTime GetDateTime (string date, int time)
   {
      if (!DateTime.TryParseExact (date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
         throw new Exception ($"Timestamp with date = '{date}' and time = '{time}' is incorrect");

      dt = dt.AddMinutes (time);
      return dt;
   }

   public void SetStartDateTime (string date, string time)
   {
      StartDate = GetDateFromString (date);
      StartTime = GetTimeFromString (time);
   }

   public void SetStartDateTime (DateTime dt)
   {
      StartDate = dt.ToString ("yyyyMMdd");
      StartTime = dt.Hour * 60 + dt.Minute;
   }

   public void SetEndDateTime (string date, string time)
   {
      EndDate = GetDateFromString (date);
      EndTime = GetTimeFromString (time);
   }

   public void SetEndDateTime (DateTime dt)
   {
      EndDate = dt.ToString ("yyyyMMdd");
      EndTime = dt.Hour * 60 + dt.Minute;
   }

   string GetDateFromString (string date)
   {
      var dates = date.Split ('.');
      if (dates == null || dates.Length != 3)
         return string.Empty;

      int day = Convert.ToInt32 (dates [0]);
      int month = Convert.ToInt32 (dates [1]);
      int year = Convert.ToInt32 (dates [2]);
      return $"{year:D4}{month:D2}{day:D2}";
   }

   int GetTimeFromString (string time)
   {
      var times = time.Split (':');
      if (times == null || times.Length != 2)
         return 0;

      int hours = Convert.ToInt32 (times [0]);
      int minutes = Convert.ToInt32 (times [1]);

      return hours * 60 + minutes;
   }

   public void SetDuration (string value)
   {
      int duration = GetDurationFromString (value.Trim ());
      DateTime dtEnd = GetStartTime ().AddMinutes (duration);
      EndDate = dtEnd.ToString ("yyyyMMdd");
      EndTime = dtEnd.Hour * 60 + dtEnd.Minute;
   }

   int GetDurationFromString (string value)
   {
      if (value.IndexOf (':') >= 0)
         return GetTimeFromString (value);

      int hours = 0, minutes = 0;
      int index = value.IndexOf ('h');
      if (index < 0)
         index = value.IndexOf ('H');

      string str;
      if (index >= 0)
      {
         str = value.Substring (0, index);
         hours = Convert.ToInt32 (str);
         str = value.Substring (index + 1).Trim ();
      }
      else
         str = value;

      index = str.IndexOf ('m');
      if (index < 0)
         index = str.IndexOf ('M');

      if (index >= 0)
      {
         str = str.Substring (0, index);
         minutes = Convert.ToInt32 (str);
      }

      return hours * 60 + minutes;
   }
}
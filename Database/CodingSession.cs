using System.Globalization;

namespace CodingTracker;

public class CodingSession
{
   public int Id { get; set; }
   public string ?StartDate { get; set; } // Format: yyyyMMdd
   public string ?EndDate { get; set; } // Format: yyyyMMdd
   public int StartTime { get; set; } // Minutes in a day, value from 0 to 1439 (23:59)
   public int EndTime { get; set; } // Minutes in a day, value from 0 to 1439 (23:59)

   public static CodingSession Create (string startDate, string startTime, string endDate, string endTime)
   {
      CodingSession session = new ();
      session.SetStartDateTime (startDate, startTime);
      session.SetEndDateTime (endDate, endTime);

      return session;
   }

   /// <summary>
   /// Will be used by filling database with some random sessions for actual project
   /// </summary>
   /// <param name="dtStart"></param>
   /// <param name="numLogs"></param>
   /// <param name="maxDurationInSeconds"></param>
   /// <returns></returns>
   public static List <CodingSession> CreateDummySessions (DateTime dtStart, int numLogs, int maxDurationInSeconds)
   {
      List<CodingSession> sessions = new ();

      var now = DateTime.Now;

      const int minPauseBetweenSessions = 10 * 60;      // 10 minutes in seconds
      const int maxPauseBetweenSessions = 12 * 60 * 60; // 12 hours in seconds

      Random rand = new ();
      for (int i = 0; i < numLogs; ++i)
      {
         DateTime dtEnd = dtStart.AddSeconds (rand.Next (1, maxDurationInSeconds));
         
         // Skip dates from future to keep generated data realistic
         if (dtStart >= now || dtEnd >= now)
            break;

         CodingSession session = new ()
         {
            StartDate = dtStart.ToString ("yyyyMMdd"),
            EndDate   = dtEnd.ToString ("yyyyMMdd"),
            StartTime = (int) dtStart.TimeOfDay.TotalSeconds,
            EndTime   = (int) dtEnd.TimeOfDay.TotalSeconds,
         };
         sessions.Add (session);

         int pause = rand.Next (minPauseBetweenSessions, maxPauseBetweenSessions);
         dtStart = dtEnd.AddSeconds (pause);
      }

      return sessions;
   }

   /// <summary>
   /// Produced object with all data of session to use in SQL-engine
   /// </summary>
   /// <param name="projectID"></param>
   /// <returns></returns>
   public object GetAnonymousObject (int projectID)
   {
      return new { ProjectId = projectID, StartDate, EndDate, StartTime, EndTime};
   }

   public string GetStartTimeAsString ()
   {
      return GetStartTime ().ToString ("dd.MM.yyyy HH:mm:ss");
   }

   public string GetEndTimeAsString ()
   {
      return GetEndTime ().ToString ("dd.MM.yyyy HH:mm:ss");
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
      int duration = (int)(GetDateTime (EndDate, EndTime) - GetDateTime (StartDate, StartTime)).TotalSeconds;
      int hours = duration / 3600;
      duration -= (hours * 3600);

      int minutes = duration / 60;
      duration -= (minutes * 60);

      return $"{hours:D2}:{minutes:D2}:{duration:D2}"; // Format "HH:mm:ss"
   }

   DateTime GetDateTime (string ?date, int time)
   {
      if (!DateTime.TryParseExact (date, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
         throw new Exception ($"Timestamp with date = '{date}' and time = '{time}' is incorrect");

      dt = dt.AddSeconds (time);
      return dt;
   }

   public string GetInfo ()
   {
      return $"{GetStartTimeAsString ()} - {GetEndTimeAsString ()}";
   }

   public void SetStartDateTime (string date, string time)
   {
      StartDate = Converter.GetDateFromString (date);
      StartTime = Converter.GetTimeFromString (time);
   }

   public void SetStartDateTime (DateTime dt)
   {
      StartDate = dt.ToString ("yyyyMMdd");
      StartTime = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
   }

   public void SetEndDateTime (string date, string time)
   {
      EndDate = Converter.GetDateFromString (date);
      EndTime = Converter.GetTimeFromString (time);
   }

   public void SetEndDateTime (DateTime dt)
   {
      EndDate = dt.ToString ("yyyyMMdd");
      EndTime = dt.Hour * 3600 + dt.Minute * 60 + dt.Second;
   }

   public void SetDuration (string value)
   {
      int duration = Converter.GetDurationFromString (value.Trim ());
      DateTime dtEnd = GetStartTime ().AddMinutes (duration);
      SetEndDateTime (dtEnd);
   }
}
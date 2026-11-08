namespace CodingTracker;
internal static class Converter
{
   /// <summary>
   /// Converts input parameter from "dd.MM.yyyy" to "yyyyMMdd". 
   /// Input parameter can be set to "now" - in this case will be taken actual date of OS 
   /// </summary>
   /// <param name="date"></param>
   /// <returns></returns>
   public static string GetDateFromString (string date)
   {
      if (date == "now")
         date = DateTime.Now.ToString ("dd.MM.yyyy");

      var dates = date.Split ('.');
      if (dates == null || dates.Length != 3)
         return string.Empty;

      int day   = Convert.ToInt32 (dates [0]);
      int month = Convert.ToInt32 (dates [1]);
      int year  = Convert.ToInt32 (dates [2]);
      return $"{year:D4}{month:D2}{day:D2}";
   }

   /// <summary>
   /// Converts input parameter from "HH:mm:ss" or "HH:mm" to seconds (formula: H x 3600 + M x 60 + S).
   /// Input parameter can be set to "now" - in this case will be taken actual time of OS 
   /// </summary>
   /// <param name="time"></param>
   /// <returns></returns>
   public static int GetTimeFromString (string time)
   {
      if (time == "now")
         time = DateTime.Now.ToString ("HH:mm:ss");

      var times = time.Split (':');
      if (times == null || times.Length < 2 || times.Length > 3)
         return 0;

      int hours   = Convert.ToInt32 (times [0]);
      int minutes = Convert.ToInt32 (times [1]);
      int seconds = times.Length == 3 ? Convert.ToInt32 (times [2]) : 0;

      return hours * 3600 + minutes * 60 + seconds;
   }

   /// <summary>
   /// Converts input parameter from "HH:mm:ss" or "HH:mm" to seconds (formula: H x 3600 + M x 60 + S).
   /// Alternative input format "'hours'h 'minutes'm 'seconds's" is also available
   /// </summary>
   /// <param name="value"></param>
   /// <returns></returns>
   public static int GetDurationFromString (string value)
   {
      // Try to parse duration in standard format "HH:mm:ss"
      if (value.IndexOf (':') >= 0)
         return GetTimeFromString (value);

      int hours = 0, minutes = 0, seconds = 0;

      // Duration is in alternative format "<hours>h <minutes>m <seconds>s".
      // Get part "<hours>" from input string
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

      // Get part "<minutes>" from input string
      index = str.IndexOf ('m');
      if (index < 0)
         index = str.IndexOf ('M');

      if (index >= 0)
      {
         str = str.Substring (0, index);
         minutes = Convert.ToInt32 (str);
         str = value.Substring (index + 1).Trim ();
      }

      index = str.IndexOf ('s');
      if (index >= 0)
      {
         str = str.Substring (0, index);
         seconds = Convert.ToInt32 (str);
      }

      return hours * 3600 + minutes * 60 + seconds;
   }
}

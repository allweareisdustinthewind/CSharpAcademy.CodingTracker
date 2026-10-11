namespace CodingTracker;

internal class Checker
{
   /// <summary>
   /// Check if the date was entered in format "dd.MM.yyyy"
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public static bool IsDateValid (string input)
   {
      var dates = input.Split ('.');
      if (dates == null || dates.Length != 3)
         return false;

      // Day
      if (dates [0].Length < 2 || !int.TryParse (dates [0], out int day) || day < 0 || day > 31)
         return false;

      // Month
      if (dates [1].Length < 2 || !int.TryParse (dates [1], out int month) || month < 0 || month > 12)
         return false;

      // Year
      if (dates [2].Length < 4 || !int.TryParse (dates [2], out int year) || year < 2000)
         return false;

      DateTime dt = new (year, month, day);
      if (dt > DateTime.Now)
         return false;

      if (month == 2) // February
      {
         bool isLeapYear = (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
         if (isLeapYear)
         {
            if (day > 29)
               return false;
         }
         else
         {
            if (day > 28)
               return false;
         }
      }
      else if (day > 30 && (month == 4 || month == 6 || month == 9 || month == 11))
         return false;

      return true;
   }

   /// <summary>
   /// Check if the time was entered in format "HH:mm"
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public static bool IsTimeValid (string input)
   {
      var times = input.Split (':');
      if (times == null || times.Length != 2)
         return false;

      // Hour (can be greater as 24)
      if (times [0].Length < 2 || !int.TryParse (times [0], out int val) || val < 0)
         return false;

      // Minute
      if (times [1].Length < 2 || !int.TryParse (times [1], out val) || val < 0 || val >= 60)
         return false;

      return true;
   }

   /// <summary>
   /// Check if the duration was entered in format "HH:mm" or "{hours}h {minutes}m"
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public static bool IsDurationValid (string input)
   {
      if (input.IndexOf (':') >= 0)
         return IsTimeValid (input);

      bool noHours = false;
      int index = input.IndexOf ('h');
      if (index < 0)
         index = input.IndexOf ('H');

      int val;
      string str;
      if (index < 0)
      {
         str = input;
         noHours = true;
      }
      else
      {
         str = input.Substring (0, index);
         if (!int.TryParse (str, out val) || val < 0)
            return false;

         str = input.Substring (index + 1).Trim ();
      }

      index = str.IndexOf ('m');
      if (index < 0)
         index = str.IndexOf ('M');

      if (index < 0 && noHours)
         return false;

      string strMin = str.Substring (0, index);
      if (!int.TryParse (strMin, out val) || val < 0)
         return false;

      str = str.Substring (index + 1).Trim ();
      if (!string.IsNullOrEmpty (str))
         return false;

      return true;
   }
}

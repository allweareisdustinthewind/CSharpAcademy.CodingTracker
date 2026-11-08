using System.Diagnostics.SymbolStore;

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
   /// Check if the time was entered in format "HH:mm" or "HH:mm:ss"
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public static bool IsTimeValid (string input)
   {
      var times = input.Split (':');
      if (times == null || times.Length < 2 || times.Length > 3)
         return false;

      // Maximal value of hours has no constraints
      int maxValue = int.MaxValue;
      foreach (var tm in times)
      {
         if (!CheckValue (tm, maxValue))
            return false;

         // Maximal value of minutes and seconds is constraints to 60 
         maxValue = 60;
      }

      bool CheckValue (string val, int maxValue = 60)
      {
         if (val.Length < 2 || !int.TryParse (val, out int res) || res < 0)
            return false;

         return res < maxValue;
      }

      return true;
   }

   /// <summary>
   /// Check if the duration was entered in format "HH:mm:ss" or "{hours}h {minutes}m {seconds}s"
   /// </summary>
   /// <param name="input"></param>
   /// <returns></returns>
   public static bool IsDurationValid (string input)
   {
      // Check data in format "HH:mm:ss"
      if (input.IndexOf (':') >= 0)
         return IsTimeValid (input);

      // Check data in format "h m s"
      var times = input.ToLower ().Split (" ");
      if (times == null || times.Length == 0 || times.Length > 3)
         return false;

      char sym = 'h';
      if (times.Length == 1) // Possible combinations: 'h', 'm' or 's'
         return CheckValue (times [0], new () { { 'h', int.MaxValue }, { 'm', 60 }, { 's', 60 } }, out sym);
      else if (times.Length == 2) // Possible combinations: pos1 - 'h' or 'm', pos2 - 'm' or 's'
      {
         if (!CheckValue (times [0], new () { { 'h', int.MaxValue } }, out sym) &&
             !CheckValue (times [0], new () { { 'm', 60 } }, out sym))
            return false;

         if (sym != 'h' && sym != 'm')
            return false;

         if (!CheckValue (times [1], new () { { 'm', 60 } }, out sym) &&
             !CheckValue (times [1], new () { { 's', 60 } }, out sym))
            return false;

         return sym == 'm' || sym == 's';
      }
      else // Possible combination: 'h', 'm', 's'
      {
         if (!CheckValue (times [0], new () { { 'h', int.MaxValue } }, out sym) ||
             !CheckValue (times [1], new () { { 'm', 60 } }, out sym) ||
             !CheckValue (times [2], new () { { 's', 60 } }, out sym))
            return false;
      }

      bool CheckValue (string data, Dictionary <char, int> allowedSymbols, out char symbol)
      {
         symbol = 'h';
         data = data.Trim ();
         if (data.Length <= 0)
            return false;

         bool isAlpha = false;
         for (int i = 0; i < data.Length; ++i)
         {
            char ch = data [i];
            if (ch >= '0' && ch <= '9')
            {
               isAlpha = true;
               continue;
            }

            if (!allowedSymbols.TryGetValue (ch, out int maxValue))
               return false;

            if (!isAlpha /*i.e. 'H1' instead of '1H'*/ || i != data.Length - 1 /*i.e. '1Hours'*/)
               return false;

            if (!int.TryParse (data.Substring (0, data.Length - 1), out int val))
               return false;

               symbol = ch;

            return val >= 0 && val < maxValue;
         }

         return false;
      }

      return true;
   }
}

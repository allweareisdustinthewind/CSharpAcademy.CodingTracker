using CodingTracker.UI.Menu;
using Spectre.Console;

namespace CodingTracker.UI;

internal class HideCursor : IDisposable
{
   bool _cursorVisibility;

   public HideCursor (bool hide = true)
   {
      _cursorVisibility = OperatingSystem.IsWindows () ? Console.CursorVisible : true;
      Console.CursorVisible = !hide;
   }

   public void Dispose ()
   {
      Console.CursorVisible = _cursorVisibility;
   }
}

/// <summary>
/// Class with some common GUI-functionality
/// </summary>
internal class Gui
{
   public static void ShowMainTitel ()
   {
      AnsiConsole.Clear ();
      Console.CursorVisible = false;
      AnsiConsole.Write ("\n       ");

      var titel = new Text ("Coding Tracker\n\n", new Style (foreground: Color.Green, decoration: Decoration.Underline));
      AnsiConsole.Write (titel);
   }

   public static bool ShowMainMenu ()
   {
      ShowMainTitel ();
      return new MainMenu ().Show ();
   }

   public static Table CreateTable (params string [] columns)
   {
      var table = new Table ().RoundedBorder ().BorderColor (Color.Aqua);

      foreach (string col in columns)
         table.AddColumn ($"[Aqua]{col}[/]", col => col.Centered ());

      return table;
   }

   public static bool Ask (string question)
   {
      using HideCursor hider = new (hide: false);
      return AnsiConsole.Confirm (question);
   }

   public static string GetUserInput (string hint)
   {
      using HideCursor hider = new (hide: false);

      AnsiConsole.Markup (hint);
      string ?value = Console.ReadLine ();
      return value ?? "";
   }

   public static void Info (string text, bool makeDelay = false)
   {
      using HideCursor hider = new ();

      var (x, y) = Console.GetCursorPosition ();
      AnsiConsole.MarkupLine (text);

      if (makeDelay)
         Thread.Sleep (2500);

      Console.SetCursorPosition (x, y);
   }

   public static void Exception (string text)
   {
      using HideCursor hider = new ();

      AnsiConsole.MarkupLine ($"[white on red] {text} [/]");
      AnsiConsole.MarkupLine ($"[yellow]Press any key to continue...[/]");
      Console.ReadKey (true);
   }

   /// <summary>
   /// Ask to enter date in format "dd.MM.yyyy". 
   /// Possible alternative answers: 
   /// - now : actual date
   /// - no data + Enter: no changes in date or cancel the changing of date 
   /// </summary>
   /// <param name="parameterName"></param>
   /// <param name="defaultValue"></param>
   /// <returns></returns>
   public static string AskDate (string parameterName = "", string defaultValue = "")
   {
      string date = string.Empty;

      for (; ; )
      {
         ShowMainTitel ();
         AnsiConsole.WriteLine ();

         if (!string.IsNullOrEmpty (parameterName))
         {
            if (!string.IsNullOrEmpty (defaultValue))
               AnsiConsole.MarkupLine ($"   Actual value of [Aqua]'{parameterName}'[/] is {defaultValue}");
            else
               AnsiConsole.MarkupLine ($"   Parameter [Aqua]'{parameterName}'[/]");
         }

         AnsiConsole.MarkupLine ("   Please enter the date in Format [Aqua]'dd.mm.yyyy'[/].");

         string str = "   You can write [blue]'now'[/] to choose actual date";
         if (!string.IsNullOrEmpty (defaultValue))
            str += " or press [blue]'Enter'[/] to make no changes in date.";
         else
            str += " or press [blue]'Enter'[/] to cancel.";

         str += "\n";
         AnsiConsole.MarkupLine (str);

         date = GetUserInput ($"   [green]Date:[/] ").Trim ();
         if (string.IsNullOrEmpty (date))
            return defaultValue;

         if (string.Compare (date.Trim (), "now", StringComparison.OrdinalIgnoreCase) == 0)
            return DateTime.Now.ToString ("dd.MM.yyyy");

         if (!Checker.IsDateValid (date))
         {
            Exception ($"The date '{date}'was entered in wrong format");
            continue;
         }

         break;
      }

      return date;
   }

   /// <summary>
   /// Ask to enter time in format "HH:mm". 
   /// Possible alternative answers: 
   /// - now : actual time
   /// - no data + Enter: no changes in time or cancel the changing of time
   /// </summary>
   /// <param name="parameterName"></param>
   /// <param name="defaultValue"></param>
   /// <returns></returns>
   public static string AskTime (string parameterName = "", string defaultValue = "")
   {
      string time = string.Empty;

      for (; ; )
      {
         ShowMainTitel ();

         AnsiConsole.WriteLine ();

         if (!string.IsNullOrEmpty (parameterName))
         { 
            if (!string.IsNullOrEmpty (defaultValue))
               AnsiConsole.MarkupLine ($"   Actual value of [Aqua]'{parameterName}'[/] is {defaultValue}");
            else
               AnsiConsole.MarkupLine ($"   Parameter [Aqua]'{parameterName}'[/]");
         }

         AnsiConsole.MarkupLine ("   Please enter the time in Format [Aqua]'hh:mm'[/].");

         string str = "   You can write [blue]'now'[/] to choose actual time";
         if (!string.IsNullOrEmpty (defaultValue))
            str += " or press [blue]'Enter' [/] to make no changes in time.";
         else
            str += " or press [blue]'Enter'[/] to cancel.";

         str += "\n";
         AnsiConsole.MarkupLine (str);

         time = GetUserInput ($"   [green]Time:[/] ").Trim ();

         if (string.IsNullOrEmpty (time))
            return defaultValue;
            
         if (string.Compare (time, "now", StringComparison.OrdinalIgnoreCase) == 0)
            return DateTime.Now.ToString ("HH:mm");
         
         if (!Checker.IsTimeValid (time))
         {
            Exception ($"The time '{time}' was entered in wrong format");
            continue;
         }

         break;
      }

      return time;
   }

   /// <summary>
   /// Ask to enter duration in format "HH:mm".
   /// Possible alternative format: "{hours}h {minutes}m", i.e. 3h 40m for 3 hours and 40 minutes.
   /// Also possible only hours or only minutes i.e. 4h, 20m, 12 H, 240    M and so on.
   /// To cancel the changing of duration just enter no data and press Enter
   /// </summary>
   /// <param name="defaultValue"></param>
   /// <returns></returns>
   public static string AskDuration (string defaultValue = "")
   {
      string duration = string.Empty;

      for (; ; )
      {
         ShowMainTitel ();

         AnsiConsole.WriteLine ();

         if (!string.IsNullOrEmpty (defaultValue))
            AnsiConsole.Markup ($"   Actual value of [Aqua]'Duration'[/] is {defaultValue}\n");

         AnsiConsole.MarkupLine ("   Please enter the duration in Format [Aqua]'hh:mm'[/].");

         string str = "   You can write [blue]'now'[/] to choose actual time";
         if (!string.IsNullOrEmpty (defaultValue))
            str += " or press [blue]'Enter' [/] to make no changes in actual duration.";
         else
            str += " or press [blue]'Enter'[/] to cancel.";

         AnsiConsole.MarkupLine (str);
         AnsiConsole.MarkupLine ($"   Alternative format: [Aqua]<hours>h <minutes>m[/], i.e. '2h 35m' for 2 hours and 35 minutes.\n");

         duration = GetUserInput ($"   [green]Duration:[/] ").Trim ();

         if (string.IsNullOrEmpty (duration))
            return defaultValue;

         if (!Checker.IsDurationValid (duration))
         {
            Exception ($"The duration '{duration}' was entered in wrong format");
            continue;
         }

         break;
      }

      return duration;
   }
}
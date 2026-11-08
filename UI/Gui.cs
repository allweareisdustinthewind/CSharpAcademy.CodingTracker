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
   /// Ask to enter time in format "HH:mm:ss". 
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

         AnsiConsole.MarkupLine ("   Please enter the time in Format [Aqua]'hh:mm'[/] or [Aqua]'hh:mm:ss'[/].");

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
            return DateTime.Now.ToString ("HH:mm:ss");
         
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
   /// Ask to enter duration in format "HH:mm:ss".
   /// Possible alternative format: "{hours}h {minutes}m {seconds}s", i.e. 3h 40m 22s for 3 hours 40 minutes 22 seconds.
   /// Also possible only hours, only minutes oor only seconds i.e. 4h, 20m, 12H, 240M, 32s and so on.
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

         AnsiConsole.MarkupLine ("   Please enter the duration in Format [Aqua]'hh:mm'[/] or [Aqua]'hh:mm:ss'[/].");
         AnsiConsole.MarkupLine ($"   Alternative format: [Aqua]<hours>h <minutes>m <seconds>s[/], i.e. '2h 35m 12s' for 2 hours 35 minutes 12 seconds.");

         string str = string.Empty;
         if (!string.IsNullOrEmpty (defaultValue))
            str += "   You can press [blue]'Enter' [/] to make no changes in actual duration.\n";
         else
            str += "   You can press [blue]'Enter'[/] to cancel.\n";

         AnsiConsole.MarkupLine (str);

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

   public static string AskFilterDateTime ()
   {
      string filter = string.Empty;

      for (; ; )
      {
         ShowMainTitel ();

         AnsiConsole.WriteLine  ();
         AnsiConsole.WriteLine  ("  Set configuration of filter to restrict values of date / time.");
         AnsiConsole.MarkupLine ("  Press [blue]Enter[/] to make no changes and go back to previous view.");
         AnsiConsole.MarkupLine ("  Allowed parameters: [Aqua]date[/] and [Aqua]time[/]");
         AnsiConsole.MarkupLine ("  Allowed operators: [blue]>, >=, <, <=, =, <>, (, ), and, or, not.[/]\n");
         AnsiConsole.MarkupLine ("  [blue]Examples:[/]\n");
         AnsiConsole.MarkupLine ($"     - ([Aqua]date[/] <= '{DateTime.Now.ToString ("dd.MM.yyyy")}' and [Aqua]date[/] > '01.02.2020') and ([Aqua]time[/] = '00:01' or [Aqua]time[/] < '02:12:25')");
         AnsiConsole.MarkupLine ($"     - [Aqua]time[/] <> '03:23' and not [Aqua]date[/] > '13.06.2025'\n");

         filter = GetUserInput ("  [green]Filter:[/] ").Trim ();
         if (string.IsNullOrEmpty (filter))
            return filter;

         break;
      }
      return filter;
   }

   public static string AskFilterDuration ()
   {
      string filter = string.Empty;

      for (; ; )
      {
         ShowMainTitel ();

         AnsiConsole.WriteLine ();
         AnsiConsole.WriteLine ("  Set configuration of filter to restrict values of duration.");
         AnsiConsole.MarkupLine ("  Press [blue]Enter[/] to make no changes and go back to previous view.");
         AnsiConsole.MarkupLine ("  Allowed parameter: [Aqua]val[/]");
         AnsiConsole.MarkupLine ("  Allowed operators: [blue]>, >=, <, <=, =, <>, (, ), and, or, not.[/]\n");
         AnsiConsole.MarkupLine ("  [blue]Examples:[/]\n");
         AnsiConsole.MarkupLine ($"     - ([Aqua]val[/] <= '5h' and [Aqua]val[/] > '03:00') and ([Aqua]val[/] = '02:06:43' or [Aqua]val[/] < '01:37:16')");
         AnsiConsole.MarkupLine ($"     - [Aqua]val[/] <> '3h 23m 14s' and not [Aqua]val[/] > '04:23'\n");

         filter = GetUserInput ("  [green]Filter:[/] ").Trim ();
         if (string.IsNullOrEmpty (filter))
            return filter;

         break;
      }
      return filter;
   }
}
using CodingTracker.UI.Menu;
using Spectre.Console;

namespace CodingTracker.UI;

/// <summary>
/// Class with some common GUI-functionality
/// </summary>
internal class Gui
{
   public static void ShowMainTitel ()
   {
      AnsiConsole.Clear ();
      AnsiConsole.Write ("\n       ");

      var titel = new Text ("Coding Tracker\n\n", new Style (foreground: Color.Green, decoration: Decoration.Underline));
      AnsiConsole.Write (titel);
   }

   public static bool ShowMainMenu ()
   {
      ShowMainTitel ();
      return new MainMenu ().Show ();
   }

   public static Spectre.Console.Table CreateTable (params string [] columns)
   {
      var table = new Spectre.Console.Table ().RoundedBorder ().BorderColor (Color.Aqua);

      foreach (string col in columns)
         table.AddColumn ($"[aqua]{col}[/]", col => col.Centered ());

      return table;
   }

   public static bool Ask (string question)
   {
      bool curVisibility = false;
      if (OperatingSystem.IsWindows ())
         curVisibility = Console.CursorVisible;

      Console.CursorVisible = true;
      bool answer = AnsiConsole.Confirm (question);
      Console.CursorVisible = curVisibility;

      return answer;
   }

   public static string GetUserInput (string hint)
   {
      bool curVisibility = false;
      if (OperatingSystem.IsWindows ())
         curVisibility = Console.CursorVisible;

      Console.CursorVisible = true;
      AnsiConsole.Markup (hint);
      string ?value = Console.ReadLine ();
      Console.CursorVisible = curVisibility;

      return value ?? "";
   }

   public static void Info (string text, bool makeDelay = true)
   {
      var (x, y) = Console.GetCursorPosition ();
      Console.CursorVisible = false;
      AnsiConsole.MarkupLine (text);

      if (makeDelay)
         Thread.Sleep (2500);

      Console.CursorVisible = true;
      Console.SetCursorPosition (x, y);
   }

   public static void Exception (string text, bool makeDelay = true)
   {
      Info ($"[white on red] {text} [/]", makeDelay);
   }
}

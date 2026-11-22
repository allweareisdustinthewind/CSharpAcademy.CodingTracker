using CodingTracker.Database;
using CodingTracker.Filter;
using CodingTracker.UI;
using Spectre.Console;

namespace CodingTracker;

internal class App
{
   public void Run ()
   {
      try
      {
         Console.CursorVisible = false;

         //--===
         //{
         //   Interpreter interpreter = new ();
         //   var expr = interpreter.Compile ("val < '1h'", false);
         //   bool res = interpreter.Run (expr, "7200");
         //   int tt = 1;
         //   tt = 2;
         //}
         //--===

         if (!SettingsHolder.Load ())
            SettingsHolder.CreateDefaultConfig ();

         Gui.ShowMainTitel ();

         if (DbManager.CreateEmptyDatabase ())
         {
            AnsiConsole.WriteLine ();
            AnsiConsole.Write ("  ");
            if (Gui.Ask ("The database has no data. Would you like to populate it with some random records?"))
            {
               AnsiConsole.Status ()
                          .Start ("Filling database...", ctx =>
                          {
                             DbManager.FillWithRandomData ();
                          });
            }
         }

         while (!Gui.ShowMainMenu ())
         {
         }
      }
      catch (Exception ex)
      {
         AnsiConsole.MarkupLine ($"[white on red] {ex.Message} [/]\n");
         AnsiConsole.MarkupLine ($"[yellow]Press any key to exit...[/]");
         Console.ReadKey (true);
      }
   }
}
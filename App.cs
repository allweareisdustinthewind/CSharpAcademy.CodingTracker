using CodingTracker.Database;
using CodingTracker.UI;
using Spectre.Console;
using CodingTracker.Filter;

namespace CodingTracker;

internal class App
{
   public void Run ()
   {
      try
      {
         //--===
         {
            FilterExpr f = new ();
            f.Evaluate ("(date >= 'now' and time <= '23:14')", "20261101", "02:21");
         }
         //--===

         Console.CursorVisible = false;

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
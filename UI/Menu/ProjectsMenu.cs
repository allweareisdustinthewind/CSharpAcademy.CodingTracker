using CodingTracker.Database;
using Spectre.Console;

namespace CodingTracker.UI.Menu;

/// <summary>
/// Menu of all habits
/// </summary>
internal class ProjectsMenu : ConsoleMenu
{
   Dictionary<string, int> _projects;

   public CodingProject CurrentProject;
   public ProjectsMenu (string title, bool onlyGetProjName = false)
      : base (title)
   {
      _projects = DbManager.GetAllProjects ().ToDictionary (x => x.Name, x => x.Id);
      if (_projects == null || _projects.Count <= 0)
      {
         Gui.Info ("There are no projects in database.");
         return;
      }

      foreach (var (name, _) in _projects)
         AddItem (name, onlyGetProjName ? GetCurrentProject : ShowProject);

      AddItem ("<Back to previous menu>", (item) => { return true; });
   }

   bool ShowProject (string projName)
   {
      GetCurrentProject (projName);
      new SessionsTable (CurrentProject).Show ();

      return false;
   }

   bool GetCurrentProject (string projName)
   {
      CurrentProject = new ()
      {
         Name = projName,
         Id = _projects [projName]
      };

      return false;
   }

   /// <summary>
   /// Display all logs of selected habit
   /// </summary>
   /// <param name="item"></param>
   /// <returns></returns>
   bool ViewLogs (string item)
   {
      //LogTable table = new (item);
      //table.Show ();
      return false;
   }

   /// <summary>
   /// Append a new log to selected habit
   /// </summary>
   /// <param name="item"></param>
   /// <returns></returns>
   bool AddLog (string item)
   {
      //HashSet<string> names = new ();

      //var habits = DbManager.GetAllHabits ();
      //string habitName = item.Trim ().ToLower ();

      //// Assign to new log ID of habit, which it belongs to
      //LogEntry? newLog = null;
      //foreach (var habit in habits)
      //{
      //   HabitEntry entry = (HabitEntry) habit;
      //   if (entry.Description.Trim ().ToLower () == habitName)
      //   {
      //      newLog = new ();
      //      newLog.HabitId = entry.Id;
      //      newLog.QuantityDesc = entry.QuantityDesc;
      //      break;
      //   }
      //}

      //if (newLog == null)
      //   return false;


      //string prevAnswer = string.Empty;

      //// Get date of log
      //for (; ; )
      //{
      //   Console.Clear ();
      //   Gui.ShowMainTitel ();
      //   AnsiConsole.MarkupLine ($"Add new log to habit [green]'{item}'[/]\n");

      //   string input = Gui.GetUserInput ("Please enter a date of new log entry in format 'dd-mm-yy' or type 'now' to use actual date: ");
      //   string date = LogEntry.ParseDate (input);
      //   if (string.IsNullOrEmpty (date))
      //   {
      //      Gui.Exception ($"The parameter was entered in incorrect format. Expected format: dd-mm-yy");
      //      continue;
      //   }

      //   newLog.Date = date;

      //   prevAnswer = "Please enter a date of new log entry in format 'dd-mm-yy' or type 'now' to use actual date: " + input;

      //   break;
      //}

      //// Get quantity, which will be logged 
      //for (; ; )
      //{
      //   int quantity = LogEntry.ParseQuantity (Gui.GetUserInput ($"Please enter quantity in {newLog.QuantityDesc} (not negative number): "));
      //   if (quantity < 0)
      //   {
      //      Gui.Exception ($"The entered parameter is incorrect. Expected not negative number");
            
      //      Console.Clear ();
      //      Gui.ShowMainTitel ();
      //      AnsiConsole.MarkupLine ($"Add new log to habit [green]'{item}'[/]\n");
      //      AnsiConsole.WriteLine (prevAnswer);

      //      continue;
      //   }

      //   newLog.Quantity = quantity;
      //   break;
      //}

      //DbManager.ExecuteCommand (newLog.cmdAdd);
      
      //Gui.Info ($"\n[green]The new log was appened to the database[/]");

      return false;
   }

   /// <summary>
   /// Show all logs of habit to delete from database
   /// </summary>
   /// <param name="item"></param>
   /// <returns></returns>
   bool DeleteLog (string item)
   {
      //LogTable table = new LogTable (item, SessionsTable.ProcesMode.Delete);
      //table.Show ();

      return false;
   }

   /// <summary>
   /// Show all logs of habit to update their data
   /// </summary>
   /// <param name="item"></param>
   /// <returns></returns>
   bool UpdateLog (string item)
   {
      //LogTable table = new LogTable (item, SessionsTable.ProcesMode.Update);
      //table.Show ();

      return false;
   }
}

using CodingTracker.Database;
using Spectre.Console;

namespace CodingTracker.UI.Menu;

/// <summary>
/// Main menu of application
/// </summary>
internal class MainMenu : ConsoleMenu
{
   public MainMenu ()
   {
      if (!string.IsNullOrEmpty (SettingsHolder.Config?.LastProject))
      {
         AddItem ($"Resume last project ('{SettingsHolder.Config.LastProject}')", ResumeLastProject);
         AddItem ("------------------------", (item) => { return false; });
      }

      AddItem ("Create new project", CreateNewProject);
      AddItem ("Start / resume existing project", StartProject);
      AddItem ("View statistic", ViewStatistic);
      AddItem ("Delete existing project", DeleteExistingProject);
      AddItem ("Exit", (item) => { return true; });
   }

   bool CreateNewProject (string item)
   {
      var projNames = DbManager.GetNamesOfAllProjects ();
      
      string name = string.Empty;
      for (; ; )
      {
         Gui.ShowMainTitel ();

         AnsiConsole.MarkupLine ("Please enter a name of your new project (no name + [blue]Enter[/] to cancel of creation).");
         name = Gui.GetUserInput ("[blue]Name:[/] ");

         string testProjName = name.Trim ().ToLower ();
         if (string.IsNullOrEmpty (testProjName))
            return false;

         if (projNames.Contains (testProjName))
         {
            Gui.Exception ("The project with this name already exists");
            continue;
         }

         break;
      }

      DbManager.AddNewProject (name);
      SettingsHolder.Config?.LastProject = name;
      SettingsHolder.Save ();

      ResumeLastProject ();

      return false;
   }

   bool StartProject (string item)
   {
      var menu = new ProjectsMenu ("Select a project to work with", onlyGetProjName: true);
      menu.Show ();

      if (menu.CurrentProject == null)
         return false;

      SettingsHolder.Config?.LastProject = menu.CurrentProject.Name;
      SettingsHolder.Save ();

      ResumeLastProject ();

      return false;
   }

   bool ResumeLastProject (string item = "")
   {
      new ProjectWorker ().Start (SettingsHolder.Config?.LastProject ?? string.Empty);
      return false;
   }

   bool ViewStatistic (string item)
   {
      new ProjectsMenu ("Select project").Show ();
      return false;
   }

   bool DeleteExistingProject (string item)
   {
      var menu = new ProjectsMenu ("Select project", onlyGetProjName: true);
      menu.Show ();

      if (menu.CurrentProject == null)
         return false;

      Gui.Info ($"The project [green]'{menu.CurrentProject.Name}'[/] with all its sessions will be deleted.");
      AnsiConsole.WriteLine ();
      if (!Gui.Ask ("Are you sure ?"))
         return false;

      DbManager.DeleteProject (menu.CurrentProject.Id);

      // If the deleted project was most recent project to work on, then get a project with latestes date
      // and set it as a new most recent project
      if (SettingsHolder.Config?.LastProject == menu.CurrentProject.Name)
         SettingsHolder.Config?.LastProject = DbManager.GetNameOfMostRecentProject ();

      return false;
   }
}
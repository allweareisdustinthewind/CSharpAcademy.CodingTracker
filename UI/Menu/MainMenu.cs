
using CodingTracker.Database;
using Spectre.Console;
using System.Xml.Linq;

namespace CodingTracker.UI.Menu;

/// <summary>
/// Main menu of application
/// </summary>
internal class MainMenu : ConsoleMenu
{
   public MainMenu ()
   {
      if (!string.IsNullOrEmpty (SettingsHolder.Config.LastProject))
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
      var projNames = DbManager.GetAllProjects ()
                               .Select (x => x.Name.Trim ().ToLower ())
                               .ToHashSet ();

      string name = string.Empty;
      for (; ; )
      {
         AnsiConsole.Clear ();
         Gui.ShowMainTitel ();

         AnsiConsole.WriteLine ("Please enter a name of your new project.");
         name = Gui.GetUserInput ("[blue]Name:[/] ");

         string testProjName = name.Trim ().ToLower ();
         if (string.IsNullOrEmpty (testProjName))
         {
            Gui.Exception ("The name can't be empty");
            continue;
         }

         if (projNames.Contains (testProjName))
         {
            Gui.Exception ("The project with this name already exists");
            continue;
         }

         break;
      }

      DbManager.AddNewProject (name);
      SettingsHolder.Config.LastProject = name;
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

      SettingsHolder.Config.LastProject = menu.CurrentProject.Name;
      SettingsHolder.Save ();

      ResumeLastProject ();

      return false;
   }

   bool ResumeLastProject (string item = "")
   {
      new ProjectWorker ().Start (SettingsHolder.Config.LastProject);
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

      if (menu.CurrentProject != null)
      {
         Gui.Info ($"The project [green]'{menu.CurrentProject.Name}'[/] with all its sessions will be deleted.", makeDelay: false);
         AnsiConsole.WriteLine ();
         if (Gui.Ask ("Are you sure ?"))
            DbManager.DeleteProject (menu.CurrentProject.Id);
      }

      return false;
   }
}

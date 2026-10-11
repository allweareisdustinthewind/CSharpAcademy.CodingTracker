using CodingTracker.Database;

namespace CodingTracker.UI.Menu;

/// <summary>
/// Menu of all projects, saved in database
/// </summary>
internal class ProjectsMenu : ConsoleMenu
{
   Dictionary<string, int> ?_projects; // Name -> Id in database

   public CodingProject ?CurrentProject; // Project, which is currently selected in menu

   public ProjectsMenu (string title, bool onlyGetProjName = false)
      : base (title)
   {
      var allProjects = DbManager.GetAllProjects ();
      if (allProjects != null)
         _projects = allProjects.ToDictionary (x => x.Name != null ? x.Name : string.Empty, x => x.Id);

      if (_projects == null || _projects.Count <= 0)
      {
         Gui.Info ("There are no projects in database.", makeDelay: true);
         return;
      }

      foreach (var (name, _) in _projects)
         AddItem (name, onlyGetProjName ? SetCurrentProject : ShowProject);

      AddItem ("<Back to previous menu>", (item) => { return true; });
   }

   bool ShowProject (string projName)
   {
      SetCurrentProject (projName);
      new SessionsTable (CurrentProject).Show ();

      return false;
   }

   bool SetCurrentProject (string projName)
   {
      if (_projects == null)
         return false;

      CurrentProject = new ()
      {
         Name = projName,
         Id = _projects [projName]
      };

      return false;
   }
}
using Spectre.Console;

namespace CodingTracker.UI.Menu;

/// <summary>
/// Wrapper around function AnsiConsole.Prompt for conveniet creation of console menu with assigned callbacks 
/// </summary>
internal class ConsoleMenu
{
   string _title;
   List<Tuple<string, MenuAction?>> _items = new ();

   public delegate bool MenuAction (string itemName);

   public ConsoleMenu (string title = "")
   { 
      _title = title;
   }

   public void AddItem (string name, MenuAction? action)
   {
      if (_items.FindIndex (x => x.Item1 == name) >= 0)
         return;

      _items.Add (Tuple.Create (name, action));
   }

   public bool Show ()
   {
      if (_items.Count <= 0)
         return false;

      var names = _items.Select (x => x.Item1).ToArray ();
      string choice = AnsiConsole.Prompt (new SelectionPrompt<string> ().Title (_title).WrapAround ().AddChoices (names));
      
      // Find and execute callback-function, which is assigned to actual selected menu's item
      int index = _items.FindIndex (x => x.Item1 == choice);
      if (index < 0)
         return false;
      
      var action = _items[index].Item2;
      return action?.Invoke (_items [index].Item1) ?? false;
   }
}
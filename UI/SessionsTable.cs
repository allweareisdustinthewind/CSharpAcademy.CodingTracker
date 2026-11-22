using CodingTracker.Database;
using CodingTracker.Filter;
using Spectre.Console;

namespace CodingTracker.UI;

/// <summary>
/// Base class to display and deal with a table on console. 
/// Contains all functionality to display tabular data and to navigate throug it
/// </summary>
internal class SessionsTable
{
   Table? _table;

   CodingProject ?_proj;
   List<CodingSession>? _sessions__;

   // Sessions, which are visible on screen. Because of filters the visual can differs from data of list '_sessions'.
   List<CodingSession> _sessionsToShow = new (); 

   HashSet<int> _changedSessions = new ();
   int _selectedLine;
   int _selectedColumn = 1;

   int _columnToSort = -1;
   int _indexLastRow = 0; 
   bool _sortInAscOrder = false;

   public SessionsTable (CodingProject ?proj)
   {
      if (proj == null)
         return;

      _proj = proj;
      _sessions__ = DbManager.GetAllSessionsOfProject (_proj.Id);

      if (_sessions__ == null || _sessions__.Count <= 0)
      {
         Gui.Info ("There are no sessions for this project.");
         return;
      }

      FillTable ();
   }

   void CreateEmptyTable ()
   {
      if (_proj == null || _proj.Name == null)
         return;

      string [] columns = ["ID", "Start", "End", "Duration"];
      string [] flags   = [string.Empty, string.Empty, string.Empty, string.Empty];
      if (_columnToSort >= 0)
         flags [_columnToSort] = _sortInAscOrder ? "asc" : "desc";

      var allFilters = SettingsHolder.Config?.Filter;
      if (allFilters != null && allFilters.TryGetValue (_proj.Name, out var filters) && filters != null)
      {
         foreach (var (column, filter) in filters)
         {
            if (filter.Expr == null)
               continue;

            string filterSymbol = filter.Active ? "[DeepPink3_1]F[/]" : "F";

            if (string.IsNullOrEmpty (flags [column]))
               flags [column] = filterSymbol;
            else
               flags [column] += $", {filterSymbol}";
         }
      }

      string [] names = columns.Zip (flags, (col, flag) => string.IsNullOrEmpty (flag) ? col : $"{col} ({flag})")
                               .ToArray ();

      _table = Gui.CreateTable (names);
   }

   void FillTable ()
   {
      CreateEmptyTable ();

      _sessionsToShow.Clear ();

      if (_sessions__ == null || _sessions__.Count <= 0 || _table == null)
         return;

      Dictionary<int, FilterExpr> filters = null;
      SettingsHolder.Config?.Filter?.TryGetValue (_proj.Name, out filters);

      _indexLastRow = -1;
      for (int i = 0; i < _sessions__.Count; ++i)
      {
         var session = _sessions__ [i];
         if (filters != null && IsFiltered (session, filters))
            continue;

         _sessionsToShow.Add (session);

         ++_indexLastRow;

         bool isChanged = _changedSessions.Contains (session.Id);
         string strID = isChanged ? $"{_indexLastRow + 1}*" : $"{_indexLastRow + 1}";
         _table.AddRow (FormatTextAsChanged (strID,                           isChanged), 
                        FormatTextAsChanged (session.GetStartTimeAsString (), isChanged), 
                        FormatTextAsChanged (session.GetEndTimeAsString (),   isChanged),
                        FormatTextAsChanged (session.GetDuration (),          isChanged));
      }

      if (_selectedLine > _indexLastRow)
         _selectedLine = _indexLastRow;
   }

   bool IsFiltered (CodingSession session, Dictionary<int, FilterExpr> filters)
   {
      if (session == null || filters == null || filters.Count <= 0)
         return false;

      foreach (var (column, filter) in filters)
      {
         if (filter == null || filter.Expr == null || !filter.Active)
            continue;

         bool leave = column switch
         {
            1 => filter.Check (session.StartDate, session.StartTime),
            2 => filter.Check (session.EndDate, session.EndTime),
            3 => filter.Check (session.GetDurationInSeconds ()),
            _ => true
         };

         if (!leave)
            return true;
      }

      return false;
   }

   string FormatTextAsChanged (string text, bool changed)
   {
      if (!changed)
         return text;

      return $"[DeepPink3_1]{text}[/]";
   }

   public void Show ()
   {
      if (_table == null)
         return;

      UpdateCurrentCell ();
      ProcessInput ();
   }

   void ShowFooter ()
   {
      var footer = new Table ().HideHeaders ().Border (TableBorder.None);
      footer.AddColumn ("Key1", col => col.RightAligned ());
      footer.AddColumn ("Desc1");
      footer.AddColumn ("Key2", col => col.RightAligned ());
      footer.AddColumn ("Desc2");

      footer.AddRow ("[blue]  Left[/]",  "- move left",                          "[blue]  Right[/]",  "- move right");
      footer.AddRow ("[blue]  Home[/]",  "- move to first record in line",       "[blue]  End[/]",    "- move to last record in line");
      footer.AddRow ("[blue]  Up[/]",    "- move up",                            "[blue]  Down[/]",   "- move down");
      footer.AddRow ("[blue]  PgUp[/]",  "- move to first record",               "[blue]  PgDown[/]", "- move to last record");
      footer.AddRow ("[blue]  Enter[/]", "- change record",                      "[blue]  Del[/]",    "- delete this session");
      footer.AddRow ("[blue]  A[/]",     "- add a new session",                  "[blue]  Space[/]",  "- sort data in active column");
      footer.AddRow ("[blue]  C[/]",     "- configure filter for active column", "[blue]  F[/]",      "- activate / deactivate filter in current column");


      if (_changedSessions.Count <= 0)
         footer.AddRow ("[blue]  Esc[/]", "- back to main menu", string.Empty,   string.Empty);
      else
         footer.AddRow ("[blue]  Esc[/]", "- back to main menu", "[DeepPink3_1]  S[/]", "[DeepPink3_1]- save all changes[/]");

      AnsiConsole.Write (footer);
      AnsiConsole.WriteLine ();
   }

   void ProcessInput ()
   {
      for (; ; )
      {
         var keyCode = Console.ReadKey (true).Key;

         switch (keyCode)
         {
            case ConsoleKey.Escape:
               if (LeaveMenu ())
                  return;

               break;

            case ConsoleKey.UpArrow:
               MoveVertical (moveUp: true);
               break;

            case ConsoleKey.DownArrow:
               MoveVertical (moveUp: false);
               break;

            case ConsoleKey.LeftArrow:
               MoveHorizontal (moveLeft: true);
               break;

            case ConsoleKey.RightArrow:
               MoveHorizontal (moveLeft: false);
               break;

            case ConsoleKey.PageDown:
               MoveVertical (moveUp: false, moveToLimit: true);
               break;

            case ConsoleKey.PageUp:
               MoveVertical (moveUp: true, moveToLimit: true);
               break;

            case ConsoleKey.Home:
               MoveHorizontal (moveLeft: true, moveToLimit: true);
               break;

            case ConsoleKey.End:
               MoveHorizontal (moveLeft: false, moveToLimit: true);
               break;

            case ConsoleKey.Delete:
               if (!DeleteCurrentSession ())
                  return;

               break;

            case ConsoleKey.Enter:
               CorrectRecord (); 
               break;

            case ConsoleKey.A:
               AddNewSession ();
               break;

            case ConsoleKey.Spacebar:
               SortData ();
               break;

            case ConsoleKey.C:
               ConfigureFilter ();
               break;

            case ConsoleKey.F:
               ToggleFilter ();
               break;

            case ConsoleKey.S:
               SaveChanges (updateGUI: true);
               break;
         }
      }
   }

   bool LeaveMenu ()
   {
      if (_changedSessions.Count <= 0)
         return true;

      Gui.Info ("There are some changed records.");
      AnsiConsole.WriteLine ();

      if (Gui.Ask ("Would you like to save the changes in database?"))
      {
         if (SaveChanges (updateGUI: false))
            return true;
      }
      else
         return true; // Exit without saving changes

      UpdateCurrentCell ();
      return false;
   }

   void AddNewSession ()
   {
      if (_proj == null || _sessions__ == null)
         return;

      string [] param  = ["Start date", "Start time", "End date", "End time"];
      List<string> values = new ();
      for (int i = 0; i < param.Length; ++i)
      {
         string data = i % 2 == 0 ? Gui.AskDate (param [i]) : Gui.AskTime (param [i]);
         if (string.IsNullOrEmpty (data))
         {
            UpdateCurrentCell ();
            return;
         }

         values.Add (data);
      }

      var session = CodingSession.Create (values [0], values [1], values [2], values [3]);

      bool isDataCorrect = ValidateSessions ([.. _sessions__, session]);
      if (isDataCorrect)
      {
         DbManager.AddNewSession (session, _proj.Id);
         _sessions__.Add (session);
         FillTable ();
      }

      UpdateCurrentCell ();
   }

   void SortData ()
   {
      if (_sessions__ == null || _sessions__.Count <= 0)
         return;

      if (_columnToSort == _selectedColumn)
         _sortInAscOrder = !_sortInAscOrder;
      else
         _columnToSort = _selectedColumn;

      if (_columnToSort == 1) // Sort by "Start time"
      {
         if (_sortInAscOrder)
            _sessions__.Sort ((x, y) => x.GetStartTime ().CompareTo (y.GetStartTime ()));
         else
            _sessions__.Sort ((x, y) => y.GetStartTime ().CompareTo (x.GetStartTime ()));
      }
      else if (_columnToSort == 2) // Sort by "End time"
      {
         if (_sortInAscOrder)
            _sessions__.Sort ((x, y) => x.GetEndTime ().CompareTo (y.GetEndTime ()));
         else
            _sessions__.Sort ((x, y) => y.GetEndTime ().CompareTo (x.GetEndTime ()));
      }
      else // Sort by "Duration"
      {
         if (_sortInAscOrder)
            _sessions__.Sort ((x, y) => x.GetDuration ().CompareTo (y.GetDuration ()));
         else
            _sessions__.Sort ((x, y) => y.GetDuration ().CompareTo (x.GetDuration ()));
      }

      FillTable ();
      UpdateCurrentCell ();
   }

   void ConfigureFilter ()
   {
      if (_proj == null || _proj.Name == null)
         return;

      bool expectDateTime = _selectedColumn == 1 || _selectedColumn == 2;
      string filterText = string.Empty;

      FilterExpr? expr = SettingsHolder.GetFilter (_proj.Name, _selectedColumn);
      string? currentConfig = expr?.Text ?? null;

      for (; ; )
      {
         filterText = expectDateTime ? Gui.AskFilterDateTime (currentConfig) : Gui.AskFilterDuration (currentConfig);
         if (string.IsNullOrEmpty (filterText))
            return;

         // No changes in text -> no need to update filter
         if (!string.IsNullOrEmpty (currentConfig) && filterText.Trim () == currentConfig)
            break;

         FilterExpr filter = new ();
         string? message = filter.SetFilterText (filterText);
         if (!string.IsNullOrEmpty (message))
         {
            Gui.Exception (message);
            continue;
         }

         SettingsHolder.SaveFilter (_proj.Name, _selectedColumn, filter);

         break;
      }

      FillTable ();
      UpdateCurrentCell ();
   }

   void ToggleFilter ()
   {
      if (_proj == null || _proj.Name == null)
         return;

      var allFilters = SettingsHolder.Config?.Filter;
      if (allFilters == null || !allFilters.TryGetValue (_proj.Name, out var filters) || filters == null)
         return;

      if (!filters.ContainsKey (_selectedColumn))
         return;

      filters [_selectedColumn].Active = !filters [_selectedColumn].Active;

      FillTable ();
      UpdateCurrentCell ();
   }

   bool SaveChanges (bool updateGUI)
   {
      if (_sessions__ == null || _changedSessions.Count <= 0)
         return true;

      var sessionsToUpdate = _sessions__.Where (x => _changedSessions.Contains (x.Id)).ToList ();
      bool isDataCorrect = ValidateSessions (sessionsToUpdate);

      if (isDataCorrect)
      {
         DbManager.UpdateSessions (sessionsToUpdate);
         _changedSessions.Clear ();
      }

      if (updateGUI)
      {
         FillTable ();
         UpdateCurrentCell ();
      }

      return isDataCorrect;
   }

   bool ValidateSessions (List <CodingSession> sessionsToCheck)
   {
      if (sessionsToCheck == null || sessionsToCheck.Count <= 0)
         return true;

      // Check, if there is a session, which starts after its ending
      var badTime = sessionsToCheck.FirstOrDefault (x => x.GetStartTime () > x.GetEndTime ());
      if (badTime != null)
      {
         Gui.Exception ($"The session {badTime.GetInfo ()} ended before starting");
         return false;
      }

      // Search for a session, which intersects with some other session
      for (int i = 0; i < sessionsToCheck.Count; ++i)
      {
         var session = sessionsToCheck [i];
         var startTime = session.GetStartTime ();
         var endTime = session.GetEndTime ();
         for (int j = i + 1; j < sessionsToCheck.Count; ++j)
         {
            var intersect = sessionsToCheck [j];
            if (intersect.GetStartTime () < endTime && intersect.GetEndTime () > startTime)
            {
               AnsiConsole.WriteLine ();
               Gui.Exception ($"The following sessions are overlapped:\n    - {session.GetInfo ()}\n    - {intersect.GetInfo ()}");
               return false;
            }
         }
      }

      return true;
   }

   /// <summary>
   /// Navigation - move actual selection from top to bottom or vise versa
   /// </summary>
   /// <param name="moveUp"></param>
   /// <param name="moveToLimit"></param>
   void MoveVertical (bool moveUp, bool moveToLimit = false)
   {
      UpdateCurrentCell (select: false);

      if (moveUp)
      {
         if (moveToLimit) // "PgUp" pressed
            _selectedLine = 0;
         else // "Up arrow" pressed
         {
            --_selectedLine;
            if (_selectedLine < 0) // Wrap around
               _selectedLine = _indexLastRow;
         }
      }
      else
      {
         if (moveToLimit) // "PgDown" pressed
            _selectedLine = _indexLastRow;
         else // "Down arrow" pressed
         {
            ++_selectedLine;
            if (_selectedLine > _indexLastRow) // Wrap around
               _selectedLine = 0;
         }
      }

      UpdateCurrentCell ();
   }

   /// <summary>
   /// Navigation - move actual selection from left to right or vise versa
   /// </summary>
   /// <param name="moveLeft"></param>
   /// <param name="moveToLimit"></param>
   void MoveHorizontal (bool moveLeft, bool moveToLimit = false)
   {
      UpdateCurrentCell (select: false);

      if (moveLeft)
      {
         if (moveToLimit) // "Home" pressed
            _selectedColumn = 1;
         else // "Left arrow" pressed
         {
            --_selectedColumn;
            if (_selectedColumn < 1) // Wrap around
               _selectedColumn = _table?.Columns.Count - 1 ?? 0;
         }
      }
      else
      {
         if (moveToLimit) // "End" pressed
            _selectedColumn = _table?.Columns.Count - 1 ?? 0;
         else // "Right arrow" pressed
         {
            ++_selectedColumn;
            if (_selectedColumn >= _table?.Columns.Count) // Wrap around
               _selectedColumn = 1;
         }
      }

      UpdateCurrentCell ();
   }

   /// <summary>
   /// Shows actial line or cell as selected
   /// </summary>
   void UpdateCurrentCell (bool select = true)
   {
      if (_table == null)
         return;

      var sessions = _sessionsToShow [_selectedLine];

      string text = _selectedColumn switch
      {
         1 => sessions.GetStartTimeAsString (),
         2 => sessions.GetEndTimeAsString (),
         3 => sessions.GetDuration (),
         _ => string.Empty
      };

      if (string.IsNullOrEmpty (text))
         return;

      bool isChanged = _changedSessions.Contains (sessions.Id);

      if (select)
         text = $"[black on yellow]{text}[/]";
      else
         text = FormatTextAsChanged (text, isChanged);

      _table.UpdateCell (_selectedLine, _selectedColumn, new Markup (text));

      Gui.ShowMainTitel ();
      AnsiConsole.Write (_table);
      ShowFooter ();
   }

   /// <summary>
   /// Deletes actual selected record from table and from database
   /// </summary>
   /// <returns></returns>
   bool DeleteCurrentSession ()
   {
      if (_sessions__ == null || _table == null)
         return true;

      Gui.Info ($"The session [green]#{_selectedLine + 1}[/] will be deleted.");
      AnsiConsole.WriteLine ();

      if (!Gui.Ask ("Are you sure?"))
      {
         UpdateCurrentCell ();
         return true;
      }

      var session = _sessionsToShow [_selectedLine];
      DbManager.DeleteSession (session.Id);
      _sessions__.RemoveAll (x => x.Id == session.Id);

      if (_sessions__.Count <= 0)
      {
         if (_proj != null)
         {
            Gui.Info ($"The project [green]'{_proj.Name}'[/] has no more sessions.");
            AnsiConsole.WriteLine ();
            if (Gui.Ask ("Would you like to delete it as well?"))
            {
               DbManager.DeleteProject (_proj.Id);
               SettingsHolder.DeleteProject (_proj.Name);
            }
         }

         return false;
      }

      if (_selectedLine >= _sessionsToShow.Count)
         _selectedLine = _sessionsToShow.Count - 1;

      FillTable ();
      UpdateCurrentCell ();

      return true;
   }

   /// <summary>
   /// Support for updating data in selected cell
   /// </summary>
   void CorrectRecord ()
   {
      if (_sessionsToShow.Count <= 0)
         return;

      bool isDataChanged = false;
      if (_selectedColumn == 1 || _selectedColumn == 2)
         isDataChanged = CorrectDateTime ();
      else
         isDataChanged = CorrectDuration ();

      if (isDataChanged)
      {
         _changedSessions.Add (_sessionsToShow [_selectedLine].Id);
         FillTable ();
      }

      UpdateCurrentCell ();
   }

   bool CorrectDateTime ()
   {
      bool isChanged = false;
      var session = _sessionsToShow [_selectedLine];

      DateTime dt;
      string nameDate, nameTime;
      if (_selectedColumn == 1)
      {
         dt = session.GetStartTime ();
         nameDate = "Start date";
         nameTime = "Start time";
      }
      else
      {
         dt = session.GetEndTime ();
         nameDate = "End date";
         nameTime = "End time";
      }

      string curDate = dt.ToString ("dd.MM.yyyy");
      string curTime = dt.ToString ("HH:mm:ss");

      // Ask user about new date / time for changed session and not allow him to enter incorrect values 
      string newDate = Gui.AskDate (nameDate, curDate);
      if (string.IsNullOrEmpty (newDate))
         return false;

      string newTime = Gui.AskTime (nameTime, curTime);
      if (string.IsNullOrEmpty (newTime))
         return false;

      isChanged = newDate != curDate || newTime != curTime;

      // Transfer entered data in actual record
      if (isChanged)
      {
         if (_selectedColumn == 1)
            session.SetStartDateTime (newDate, newTime);
         else
            session.SetEndDateTime (newDate, newTime);
      }

      return isChanged;
   }

   bool CorrectDuration ()
   {
      var session = _sessionsToShow [_selectedLine];

      string prevDuration = session.GetDuration ();
      string newDuration = Gui.AskDuration (prevDuration);
      if (string.IsNullOrEmpty (newDuration))
         return false;

      session.SetDuration (newDuration);
      return session.GetDuration () != prevDuration;
   }
}
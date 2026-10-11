using CodingTracker.Database;
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
   List<CodingSession>? _sessions;
   HashSet<int> _changedSessions = new ();
   int _selectedLine;
   int _selectedColumn = 1;

   int _columnToSort = -1;
   bool _sortInAscOrder = false;

   public SessionsTable (CodingProject ?proj)
   {
      if (proj == null)
         return;

      _proj = proj;
      _sessions = DbManager.GetAllSessionsOfProject (_proj.Id);

      if (_sessions == null || _sessions.Count <= 0)
      {
         Gui.Info ("There are no sessions for this project.");
         return;
      }

      FillTable ();
   }

   void CreateEmptyTable ()
   {
      string [] columns = ["ID", "Start", "End", "Duration (hh:mm)"];
      if (_columnToSort >= 0)
      {
         string str = _sortInAscOrder ? " (asc)" : " (desc)";
         columns [_columnToSort] += str;
      }

      _table = Gui.CreateTable (columns);
   }

   void FillTable ()
   {
      CreateEmptyTable ();

      if (_sessions == null || _sessions.Count <= 0)
         return;

      for (int i = 0; i < _sessions.Count; ++i)
      {
         var session = _sessions [i];
         bool isChanged = _changedSessions.Contains (session.Id);
         string strID = isChanged ? $"{i + 1}*" : $"{i + 1}";
         _table.AddRow (FormatTextAsChanged (strID,                           isChanged), 
                        FormatTextAsChanged (session.GetStartTimeAsString (), isChanged), 
                        FormatTextAsChanged (session.GetEndTimeAsString (),   isChanged),
                        FormatTextAsChanged (session.GetDuration (),          isChanged));
      }
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
      if (_proj == null || _sessions == null)
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

      bool isDataCorrect = ValidateSessions ([.. _sessions, session]);
      if (isDataCorrect)
      {
         DbManager.AddNewSession (session, _proj.Id);
         _sessions.Add (session);
         FillTable ();
      }

      UpdateCurrentCell ();
   }

   void SortData ()
   {
      if (_sessions == null || _sessions.Count <= 0)
         return;

      if (_columnToSort == _selectedColumn)
         _sortInAscOrder = !_sortInAscOrder;
      else
         _columnToSort = _selectedColumn;

      if (_columnToSort == 1) // Sort by "Start time"
      {
         if (_sortInAscOrder)
            _sessions.Sort ((x, y) => x.GetStartTime ().CompareTo (y.GetStartTime ()));
         else
            _sessions.Sort ((x, y) => y.GetStartTime ().CompareTo (x.GetStartTime ()));
      }
      else if (_columnToSort == 2) // Sort by "End time"
      {
         if (_sortInAscOrder)
            _sessions.Sort ((x, y) => x.GetEndTime ().CompareTo (y.GetEndTime ()));
         else
            _sessions.Sort ((x, y) => y.GetEndTime ().CompareTo (x.GetEndTime ()));
      }
      else // Sort by "Duration"
      {
         if (_sortInAscOrder)
            _sessions.Sort ((x, y) => x.GetDuration ().CompareTo (y.GetDuration ()));
         else
            _sessions.Sort ((x, y) => y.GetDuration ().CompareTo (x.GetDuration ()));
      }

      FillTable ();
      UpdateCurrentCell ();
   }

   bool SaveChanges (bool updateGUI)
   {
      if (_sessions == null || _changedSessions.Count <= 0)
         return true;

      var sessionsToUpdate = _sessions.Where (x => _changedSessions.Contains (x.Id)).ToList ();
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
               _selectedLine = _sessions?.Count - 1 ?? 0;
         }
      }
      else
      {
         if (moveToLimit) // "PgDown" pressed
            _selectedLine = _sessions?.Count - 1 ?? 0;
         else // "Down arrow" pressed
         {
            ++_selectedLine;
            if (_selectedLine >= _sessions?.Count) // Wrap around
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
      if (_sessions == null || _table == null)
         return;

      var sessions = _sessions [_selectedLine];

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
      if (_sessions == null || _table == null)
         return true;

      Gui.Info ($"The session [green]#{_selectedLine + 1}[/] will be deleted.");
      AnsiConsole.WriteLine ();

      if (!Gui.Ask ("Are you sure?"))
      {
         UpdateCurrentCell ();
         return true;
      }

      var session = _sessions [_selectedLine];
      DbManager.DeleteSession (session.Id);
      _sessions.RemoveAt (_selectedLine);

      if (_sessions.Count <= 0)
      {
         if (_proj != null)
         {
            Gui.Info ($"The project [green]'{_proj.Name}'[/] has no more sessions.");
            AnsiConsole.WriteLine ();
            if (Gui.Ask ("Would you like to delete it as well?"))
               DbManager.DeleteProject (_proj.Id);
         }

         return false;
      }

      if (_selectedLine >= _sessions.Count)
         _selectedLine = _sessions.Count - 1;

      FillTable ();
      UpdateCurrentCell ();

      return true;
   }

   /// <summary>
   /// Support for updating data in selected cell
   /// </summary>
   void CorrectRecord ()
   {
      if (_sessions == null || _sessions.Count <= 0)
         return;

      bool isDataChanged = false;
      if (_selectedColumn == 1 || _selectedColumn == 2)
         isDataChanged = CorrectDateTime ();
      else
         isDataChanged = CorrectDuration ();

      if (isDataChanged)
      {
         _changedSessions.Add (_sessions [_selectedLine].Id);
         FillTable ();
      }

      UpdateCurrentCell ();
   }

   bool CorrectDateTime ()
   {
      if (_sessions == null)
         return true;

      bool isChanged = false;
      var session = _sessions [_selectedLine];

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
      string curTime = dt.ToString ("HH:mm");

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
      if (_sessions == null)
         return true;

      var session = _sessions [_selectedLine];

      string prevDuration = session.GetDuration ();
      string newDuration = Gui.AskDuration (prevDuration);
      if (string.IsNullOrEmpty (newDuration) || newDuration == prevDuration)
         return false;

      session.SetDuration (newDuration);
      return true;
   }
}
using CodingTracker.Database;
using Microsoft.VisualBasic;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace CodingTracker.UI;

/// <summary>
/// Base class to display and deal with a table on console. 
/// Contains all functionality to display tabular data and to navigate throug it
/// </summary>
internal class SessionsTable
{
   Table? _table;

   CodingProject _proj;
   List<CodingSession>? _sessions;
   HashSet<int> _changedSessions = new ();
   int _selectedLine;
   int _selectedColumn = 1;

   public SessionsTable (CodingProject proj)
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

      CreateEmptyTable ();
      FillTable ();
   }

   void CreateEmptyTable ()
   {
      _table = Gui.CreateTable ([ "ID", "Start", "End", "Duration (hh:mm)"]);
   }

   void FillTable ()
   {
      if (_table == null || _sessions == null || _sessions.Count <= 0)
         return;

      for (int i = 0; i < _sessions.Count; ++i)
      {
         var session = _sessions [i];
         bool isChanged = _changedSessions.Contains (session.Id);
         string strID = isChanged ? $"{i + 1}*" : $"{i + 1}";
         _table.AddRow (FormatText (strID,                           isChanged), 
                        FormatText (session.GetStartTimeAsString (), isChanged), 
                        FormatText (session.GetEndTimeAsString (),   isChanged),
                        FormatText (session.GetDuration (),          isChanged));
      }
   }

   string FormatText (string text, bool changed)
   {
      if (!changed)
         return text;

      return $"[DeepPink3_1]{text}[/]";
   }

   public void Show ()
   {
      if (_table == null)
         return;

      UpdateCurrentCell (select: true);
      ProcessInput ();
   }

   void ShowFooter ()
   {
      var footer = new Table ().HideHeaders ().Border (TableBorder.None);
      footer.AddColumn ("Key1", col => col.RightAligned ());
      footer.AddColumn ("Desc1");
      footer.AddColumn ("Key2", col => col.RightAligned ());
      footer.AddColumn ("Desc2");

      footer.AddRow ("[blue]  Left[/]",  "- move left",                    "[blue]  Right[/]",  "- move right");
      footer.AddRow ("[blue]  Home[/]",  "- move to first record in line", "[blue]  End[/]",    "- move to last record in line");
      footer.AddRow ("[blue]  Up[/]",    "- move up",                      "[blue]  Down[/]",   "- move down");
      footer.AddRow ("[blue]  PgUp[/]",  "- move to first record",         "[blue]  PgDown[/]", "- move to last record");
      footer.AddRow ("[blue]  Enter[/]", "- change record",                "[blue]  Del[/]",    "- delete this session");

      if (_changedSessions.Count <= 0)
         footer.AddRow ("[blue]  Esc[/]", "- back to main menu", string.Empty,   string.Empty);
      else
         footer.AddRow ("[blue]  Esc[/]", "- back to main menu", "[DeepPink3_1]  S[/]", "[DeepPink3_1]- save all changes[/]");

      AnsiConsole.Write (footer);
      AnsiConsole.WriteLine ();
   }

   void ProcessInput ()
   {
      Console.CursorVisible = false;

      for (; ; )
      {
         var keyCode = Console.ReadKey (true).Key;
         if (keyCode == ConsoleKey.Escape)
         {
            if (_changedSessions.Count > 0)
            {
               Gui.Info ("There are some changed records.", makeDelay: false);
               AnsiConsole.WriteLine ();

               if (Gui.Ask ("Would you like to save the changes in database?"))
               {
                  if (SaveChanges (updateGUI: false))
                     break;
               }
            }
            else
               break;

            UpdateCurrentCell (select: true);
         }

         switch (keyCode)
         {
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

            case ConsoleKey.S:
               SaveChanges (updateGUI: true);
               break;
         }
      }
   }

   bool SaveChanges (bool updateGUI)
   {
      if (_changedSessions.Count <= 0)
         return true;

      var sessionsToUpdate = _sessions.Where (x => _changedSessions.Contains (x.Id)).ToList ();

      if (ValidateChangedRecords (sessionsToUpdate))
      {
         DbManager.UpdateSessions (sessionsToUpdate);
         _changedSessions.Clear ();
      }

      if (updateGUI)
      {
         CreateEmptyTable ();
         FillTable ();
         UpdateCurrentCell (select: true);
      }

      return true;
   }

   bool ValidateChangedRecords (List <CodingSession> sessionsToCheck)
   {
      if (sessionsToCheck == null || sessionsToCheck.Count <= 0)
         return true;

      var times = sessionsToCheck.Select (x => new { x.Id, StartTime = x.GetStartTime (), EndTime = x.GetEndTime () }).ToList ();
      var badTime = times.FirstOrDefault (x => x.StartTime >= x.EndTime);
      if (badTime != null)
      {
         Gui.Exception ($"Start time of session {badTime.Id} is more or equal as its end time");
         return false;
      }

      foreach (var session in _sessions)
      {
         if (_changedSessions.Contains (session.Id))
            continue;

         var intersect = times.FirstOrDefault (x => session.GetEndTime () >= x.StartTime && session.GetStartTime () <= x.EndTime);
         if (intersect == null)
            continue;

         Gui.Exception ($"False date / time: the sessions with ids {intersect.Id} and {session.Id} are overlapped");
         return false;
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

      UpdateCurrentCell (select: true);
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

      UpdateCurrentCell (select: true);
   }

   /// <summary>
   /// Shows actial line or cell as selected
   /// </summary>
   void UpdateCurrentCell (bool select)
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
         text = FormatText (text, isChanged);

      _table.UpdateCell (_selectedLine, _selectedColumn, new Markup (text));

      AnsiConsole.Clear ();
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

      Gui.Info ($"The session [green]#{_selectedLine + 1}[/] will be deleted.", makeDelay: false);
      AnsiConsole.WriteLine ();

      if (!Gui.Ask ("Are you sure?"))
      {
         UpdateCurrentCell (select: true);
         return true;
      }

      var session = _sessions [_selectedLine];
      DbManager.DeleteSession (session.Id);

      _sessions.RemoveAt (_selectedLine);
      if (_sessions.Count <= 0)
      {
         if (_proj != null)
         {
            Gui.Info ($"The project [green]'{_proj.Name}'[/] has no more sessions.", makeDelay: false);
            AnsiConsole.WriteLine ();
            if (Gui.Ask ("Would you like to delete it as well?"))
               DbManager.DeleteProject (_proj.Id);
         }

         return false;
      }

      if (_selectedLine >= _sessions.Count)
         _selectedLine = _sessions.Count - 1;

      CreateEmptyTable ();
      FillTable ();
      UpdateCurrentCell (select: true);

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
         CreateEmptyTable ();
         FillTable ();
      }

      UpdateCurrentCell (select: true);
   }

   bool CorrectDateTime ()
   {
      bool isChanged = false;
      var session = _sessions [_selectedLine];

      DateTime dt;
      string name, value;
      if (_selectedColumn == 1)
      {
         dt = session.GetStartTime ();
         name = "Start time";
         value = session.GetStartTimeAsString ();
      }
      else
      {
         dt = session.GetEndTime ();
         name = "End time";
         value = session.GetEndTimeAsString ();
      }

      string prevDate = dt.ToString ("dd.MM.yyyy");
      string prevTime = dt.ToString ("HH:mm");

      string newDate = string.Empty;
      string newValue;

      // Ask a new data until all inputs will be correct  
      for (; ; )
      {
         Console.Clear ();
         Gui.ShowMainTitel ();

         AnsiConsole.WriteLine ();
         AnsiConsole.Markup ($"Actual value of property [green]'{name}'[/]: {value}\n");

         if (!string.IsNullOrEmpty (newDate))
            AnsiConsole.MarkupLine ($"[green]Entered date:[/] {newDate}");
         else
         {
            AnsiConsole.MarkupLine ("Please enter the date in Format [green]'dd.mm.yyyy'[/].");
            AnsiConsole.MarkupLine ("You can write [green]'now'[/] to choose actual date or press [green]'Enter'[/] to make no changes in date.");
            newValue = Gui.GetUserInput ($"New date: ");
            if (string.IsNullOrEmpty (newValue))
               newValue = prevDate;
            else if (string.Compare (newValue.Trim (), "now", StringComparison.OrdinalIgnoreCase) == 0)
               newValue = DateTime.Now.ToString ("dd.MM.yyyy");
            else
            {
               if (!CheckDate (newValue.Trim ()))
               {
                  Gui.Exception ("The date was entered in wrong format");
                  continue;
               }
            }

            newDate = newValue.Trim ();
            if (newDate != prevDate)
               isChanged = true;

            continue;
         }

         AnsiConsole.MarkupLine ("Please enter the time in Format [green]'hh:mm'[/].");
         AnsiConsole.MarkupLine ("You can write [green]'now'[/] to choose actual time or press [green]'Enter'[/] to make no changes in time.");
         newValue = Gui.GetUserInput ($"New time: ");

         if (string.IsNullOrEmpty (newValue))
            newValue = prevTime;
         else if (string.Compare (newValue.Trim (), "now", StringComparison.OrdinalIgnoreCase) == 0)
            newValue = DateTime.Now.ToString ("HH:mm");
         else
         {
            if (!CheckTime (newValue.Trim ()))
            {
               Gui.Exception ("The time was entered in wrong format");
               continue;
            }
         }

         newValue = newValue.Trim ();
         if (newValue != prevTime)
            isChanged = true;

         // Transfer entered data in actual record
         if (isChanged)
         {
            if (_selectedColumn == 1)
               session.SetStartDateTime (newDate, newValue);
            else
               session.SetEndDateTime (newDate, newValue);
         }

         break;
      }

      return isChanged;
   }

   bool CorrectDuration ()
   {
      bool isChanged = false;
      var session = _sessions [_selectedLine];

      string prevDuration = session.GetDuration ();

      // Ask a new data until all inputs will be correct  
      for (; ; )
      {
         Console.Clear ();
         Gui.ShowMainTitel ();

         AnsiConsole.WriteLine ();
         AnsiConsole.MarkupLine ($"Actual value of property [green]'Duration'[/]: {prevDuration}");
         AnsiConsole.MarkupLine ($"Please enter new duration in format [green]'hh:mm'[/] or press [green]'Enter'[/] to make no changes.");
         AnsiConsole.MarkupLine ($"Alternative format: [Aqua]<hours>h <minutes>m[/], i.e. '2h 35m' for 2 hours and 35 minutes.");

         string newValue = Gui.GetUserInput ($"New duration: ");
         if (string.IsNullOrEmpty (newValue))
            break;

         if (!CheckDuration (newValue.Trim ()))
         {
            Gui.Exception ("The duration was entered in wrong format");
            continue;
         }

         if (newValue != prevDuration)
         {
            session.SetDuration (newValue);
            isChanged = true;
         }

         break;
      }

      return isChanged;
   }

   bool CheckDate (string input)
   {
      var dates = input.Split ('.');
      if (dates == null || dates.Length != 3)
         return false;

      if (dates [0].Length < 2 || !int.TryParse (dates [0], out int day) || day < 0 || day > 31)
         return false;

      if (dates [1].Length < 2 || !int.TryParse (dates [1], out int month) || month < 0 || month > 12)
         return false;

      if (dates [2].Length < 4 || !int.TryParse (dates [2], out int year) || year < 2000)
         return false;

      DateTime dt = new (year, month, day);
      if (dt > DateTime.Now)
         return false;

      if (month == 2) // February
      {
         bool isLeapYear = (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
         if (isLeapYear)
         {
            if (day > 29)
               return false;
         }
         else
         {
            if (day > 28)
               return false;
         }
      }
      else if (day > 30 && (month == 4 || month == 6 || month == 9 || month == 11))
         return false;

      return true;
   }

   bool CheckTime (string input)
   {
      var times = input.Split (':');
      if (times == null || times.Length != 2)
         return false;

      if (times [0].Length < 2 || !int.TryParse (times [0], out int val) || val < 0)
         return false;

      if (times [1].Length < 2 || !int.TryParse (times [1], out val) || val < 0 || val >= 60)
         return false;

      return true;
   }

   bool CheckDuration (string input)
   {
      if (input.IndexOf (':') >= 0)
         return CheckTime (input);

      bool noHours = false;
      int index = input.IndexOf ('h');
      if (index < 0)
         index = input.IndexOf ('H');

      int val;
      string str;
      if (index < 0)
      {
         str = input;
         noHours = true;
      }
      else
      {
         str = input.Substring (0, index);
         if (!int.TryParse (str, out val) || val < 0)
            return false;

         str = input.Substring (index + 1).Trim ();
      }

      index = str.IndexOf ('m');
      if (index < 0)
         index = str.IndexOf ('M');

      if (index < 0 && noHours)
         return false;

      string strMin = str.Substring (0, index);
      if (!int.TryParse (strMin, out val) || val < 0)
         return false;

      str = str.Substring (index + 1).Trim ();
      if (!string.IsNullOrEmpty (str))
         return false;

      return true;
   }
}

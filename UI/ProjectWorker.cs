using CodingTracker.Database;
using Spectre.Console;

namespace CodingTracker.UI;

internal class ProjectWorker
{
   enum Status
   {
      Paused,
      Working
   }

   int _projID;

   #region Positions on screen to update dynamicaly changed values of statistic 
   int _posXLastPause;
   int _posYLastPause;

   int _posXDurationSincePause;
   int _posYDurationSincePause;

   int _posXTimeTotal;
   int _posYTimeTotal;

   int _posXStatus;
   int _posYStatus;

   int _posXSpinner;
   int _posYSpinner;
   #endregion

   Status _currentStatus = Status.Paused;
   Timer ?_timer;

   string _spinner = "\\|/-";
   int _spinnerIndex;

   DateTime _dtStartWork = DateTime.MinValue;
   DateTime _dtLastPause = DateTime.MinValue;

   int _totalSeconds; // Total amount of working time since start working on project

   public void Start (string projName)
   {
      _projID = DbManager.GetProjectID (projName);

      Gui.ShowMainTitel ();
      ShowStatistic (projName);
      UpdateActualStatus ();
      ShowFooter ();
      ProcessInput ();
   }

   void ShowStatistic (string projName)
   {
      AnsiConsole.MarkupLine ($"    [blue]Statistic[/]");
      AnsiConsole.MarkupLine ($"    [blue]-------------------------[/]");
      AnsiConsole.MarkupLine ($"    Project name: [green]           {projName}[/]");
      AnsiConsole.MarkupLine ($"    Started at:              [yellow]{DateTime.Now.ToString ("dd.MM.yyyy HH:mm:ss")}[/]");
      AnsiConsole.Markup     ($"    Last pause at:           ");
      (_posXLastPause, _posYLastPause) = Console.GetCursorPosition ();
      AnsiConsole.WriteLine ();
      AnsiConsole.Markup     ($"    Worked since last pause: ");
      (_posXDurationSincePause, _posYDurationSincePause) = Console.GetCursorPosition ();
      AnsiConsole.WriteLine ();
      AnsiConsole.Markup ($"    Total working time:      ");
      (_posXTimeTotal, _posYTimeTotal) = Console.GetCursorPosition ();
      AnsiConsole.MarkupLine ("[yellow]0h 0m 0s\n[/]");

      (_posXStatus, _posYStatus) = Console.GetCursorPosition ();
   }

   void UpdateActualStatus ()
   {
      Console.SetCursorPosition (_posXStatus, _posYStatus);
      AnsiConsole.Markup ("                                      ");
      Console.SetCursorPosition (_posXStatus, _posYStatus);

      string text = _currentStatus switch
      {
         Status.Paused => "[Gray on DarkRed] Paused [/]",
         Status.Working => "[white on green] Working... [/]",
         _ => string.Empty
      };

      AnsiConsole.Markup ($"    Actual status: {text} ");
      (_posXSpinner, _posYSpinner) = Console.GetCursorPosition ();
   }

   void ShowFooter ()
   {
      AnsiConsole.MarkupLine ("\n\n\n    Please use following keys to control tracking:");
      AnsiConsole.MarkupLine ("      [blue]Space[/] - Pause / Resume");
      AnsiConsole.MarkupLine ("      [blue]Esc[/]   - Break work and go to main menu");
   }

   void ProcessInput ()
   {
      for (; ; )
      {
         var keyCode = Console.ReadKey (true).Key;
         switch (keyCode)
         {
            case ConsoleKey.Escape:
               StopWork ();
               return;

            case ConsoleKey.Spacebar:
               ToggleActualStatus ();
               break;
         }
      }
   }

   void StopWork ()
   {
      StopTimer ();

      if (_currentStatus == Status.Working)
         SaveCurrentSession ();
   }

   void ToggleActualStatus ()
   {
      if (_currentStatus == Status.Paused)
      {
         _currentStatus = Status.Working;
         _dtStartWork = DateTime.Now;
         UpdateActualStatus ();

         _timer = new (TimerFunction, null, 0, 1000);
         return;
      }
      
      StopTimer ();
      SaveCurrentSession ();

      int duration = (int) (DateTime.Now - _dtStartWork).TotalSeconds;
      _totalSeconds += duration;

      _currentStatus = Status.Paused;
      _dtLastPause = DateTime.Now;
      UpdateLastPauseInfo ();
      UpdateActualStatus ();
   }

   void StopTimer ()
   {
      _timer?.Dispose ();
      _timer = null;
      GC.Collect ();
   }

   void SaveCurrentSession ()
   {
      CodingSession session = new ();
      session.SetStartDateTime (_dtStartWork);
      session.SetEndDateTime (DateTime.Now);

      DbManager.AddNewSession (session, _projID);
   }

   void UpdateLastPauseInfo ()
   {
      Console.SetCursorPosition (_posXLastPause, _posYLastPause);
      AnsiConsole.Markup ($"[yellow]{_dtLastPause.ToString ("dd.MM.yyyy HH:mm:ss")}[/]");
   }

   void UpdateTimes ()
   {
      string text;
      int duration = (int) (DateTime.Now - _dtStartWork).TotalSeconds;
      if (_dtLastPause != DateTime.MinValue)
      {
         Console.SetCursorPosition (_posXDurationSincePause, _posYDurationSincePause);
         AnsiConsole.Write ("                    ");
         text = FormatDuration (duration);
         Console.SetCursorPosition (_posXDurationSincePause, _posYDurationSincePause);
         AnsiConsole.Markup (text);
      }

      text = FormatDuration (_totalSeconds + duration);
      Console.SetCursorPosition (_posXTimeTotal, _posYTimeTotal);
      AnsiConsole.Markup (text);
   }

   string FormatDuration (int seconds)
   {
      int hours = seconds / 3600;
      seconds %= 3600;

      int minutes = seconds / 60;
      seconds %= 60;

      return $"[yellow]{hours}h {minutes}m {seconds}s[/]";
   }

   void TimerFunction (Object ?obj)
   {
      Console.SetCursorPosition (_posXSpinner, _posYSpinner);
      AnsiConsole.Write (_spinner [_spinnerIndex++]);
      if (_spinnerIndex >= _spinner.Length)
         _spinnerIndex = 0;

      UpdateTimes ();
   }
}
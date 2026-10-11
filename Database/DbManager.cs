using Dapper;
using Microsoft.Data.Sqlite;

namespace CodingTracker.Database;

/// <summary>
/// Class to manage all CRUD operations with database
/// </summary>
public class DbManager
{
   public static SqliteConnection EstablishConnection (string connectionString)
   {
      SqliteConnection connection = new (connectionString);
      connection.Open ();
      return connection;
   }

   public static SqliteConnection EstablishDefaultConnection ()
   { 
      return EstablishConnection (SettingsHolder.Config?.ConnectionString ?? string.Empty);
   }

   /// <summary>
   /// Establish connection with database and execute arbitrary command on it
   /// </summary>
   /// <param name="cmd"></param>
   static void Execute (Action <SqliteConnection ?> cmd)
   {
      if (cmd == null)
         return;

      SqliteConnection ?connection = null;
      try
      {
         connection = EstablishDefaultConnection ();
         cmd (connection);
      }
      catch (Exception)
      {
         throw;
      }
      finally
      {
         connection?.Close ();
      }
   }

   public static bool CreateEmptyDatabase ()
   {
      bool newDatabase = false;
      Execute ((connection) =>
      {
         if (connection == null)
            return;

         connection.Execute (@"CREATE TABLE IF NOT EXISTS projects
                               (Id INTEGER PRIMARY KEY AUTOINCREMENT,
                               Name TEXT UNIQUE NOT NULL)");

         connection.Execute (@"CREATE TABLE IF NOT EXISTS logs 
                               (Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ProjectId INTEGER, 
                                StartDate TEXT NOT NULL,
                                EndDate TEXT NOT NULL,
                                StartTime INTEGER,
                                EndTime INTEGER,
                                FOREIGN KEY (ProjectId) REFERENCES projects(Id)
                                ON DELETE CASCADE)");

         var count = connection.ExecuteScalar ("SELECT COUNT (*) cnt FROM projects");
         newDatabase = count == null || count.ToString () == "0";
      });

      return newDatabase;
   }

   public static void FillWithRandomData ()
   {
      // Some random projects to append to database 
      List<string> projectNames = new ()
      {
         "Project 'Helix'",
         "Project 'Eclipse'",
         "Homework #42",
         "Menu for game 'Last hunter of Altai'",
         "Excercises in Ruby",
         "Learn Go",
         "Web-Server in C#",
         "Reverse engineering of some program",
         "Study-project for my wife",
         "Recreating DOS-game from scratch on windows",
         "Unspecified"
      };

      const int maxProjects          = 5;
      const int maxLogsPerProject    = 20;
      const int maxDurationInMinutes = 300; // 5 hours
      const int workPeriodInDays     = 30;

      DateTime dtStart = DateTime.Now.AddDays (-workPeriodInDays);
      Random rand = new ();

      // To search a project with latest date (most recent project from latest work)
      DateTime lastTime = DateTime.MinValue;
      string lastProject = string.Empty;

      Dictionary <string, List<CodingSession>> allSessions = new ();
      for (int i = 0; i < maxProjects; ++i)
      {
         string projectName;
         for (; ; )
         {
            int projIndex = rand.Next (0, projectNames.Count);
            projectName = projectNames [projIndex];
            if (!allSessions.ContainsKey (projectName))
               break;
         }

         // Generate start date and time of work on the project
         DateTime dt = dtStart.AddDays (rand.Next (0, workPeriodInDays));
         dt = dt.AddMinutes (rand.Next (0, 1440));

         int numLogs = rand.Next (1, maxLogsPerProject);

         var sessions = CodingSession.CreateDummySessions (dt, numLogs, maxDurationInMinutes);
         if (sessions == null || sessions.Count <= 0)
            continue;

         // If its a project with latest time, then save it as a most recent project
         DateTime lastTimeCurSessions = sessions [sessions.Count - 1].GetEndTime ();
         if (lastTimeCurSessions > lastTime)
         { 
            lastTime = lastTimeCurSessions;
            lastProject = projectName;
         }

         allSessions [projectName] = sessions;
      }

      if (allSessions.Count <= 0)
         throw new Exception ("Issue by filling database with some random data, check the logic of program");

      if (!string.IsNullOrEmpty (lastProject))
      { 
         SettingsHolder.Config?.LastProject = lastProject;
         SettingsHolder.Save ();
      }

      Execute ((connection) =>
      {
         if (connection == null)
            return;

         // Write names of all projects to db
         var namesAllProjects = allSessions.Keys.ToList ().Select (x => new { Name = x }).ToList ();
         connection.Execute ("INSERT INTO projects (Name) VALUES (@Name)", namesAllProjects);

         List<CodingProject> projects = connection.Query<CodingProject> ("SELECT * FROM projects").ToList ();

         // Write sessions of all projects to db
         List<object> sessionsToWrite = new ();

         foreach (var proj in projects)
         {
            if (proj == null || proj.Name == null)
               continue;

            var sessions = allSessions [proj.Name];
            sessionsToWrite.AddRange (sessions.Select (x => x.GetAnonymousObject (proj.Id)));
         }

         connection.Execute (@"INSERT INTO logs (ProjectId, StartDate, EndDate, StartTime, EndTime) 
                               VALUES (@ProjectId, @StartDate, @EndDate, @StartTime, @EndTime)",
                             sessionsToWrite);
      });
   }

   public static List<CodingProject> ?GetAllProjects ()
   {
      List<CodingProject> ?projects = null;

      Execute ((connection) =>
      {
         projects = connection?.Query<CodingProject> ("SELECT * FROM projects ORDER BY Name")
                               .ToList ();
      });

      return projects;
   }

   public static HashSet<string> GetNamesOfAllProjects ()
   {
      HashSet<string> names = new ();
      var projects = GetAllProjects ();
      if (projects == null)
         return names;

      names = projects.Select (x => x.Name != null ? x.Name.Trim ().ToLower () : string.Empty)
                      .ToHashSet ();
      return names;
   }

   public static List<CodingSession> ?GetAllSessionsOfProject (int projID)
   {
      List<CodingSession> ?sessions = null;
      
      Execute ((connection) => 
      {
         string sql = $"SELECT * FROM logs WHERE ProjectId = @ProjectId ORDER BY StartDate, StartTime";
         sessions = connection?.Query<CodingSession> (sql, new { ProjectId = projID})
                               .ToList ();
      });

      return sessions;
   }

   public static void DeleteSession (int sessionID)
   {
      Execute ((connection) =>
      {
         connection?.Execute ($"DELETE FROM logs WHERE Id = @Id", new { Id = sessionID });
      });
   }

   public static void UpdateSessions (List<CodingSession> sessions)
   {
      if (sessions == null || sessions.Count <= 0)
         return;

      Execute ((connection) =>
      {
         if (connection == null)
            return;

         foreach (var session in sessions)
         {
            var param = new
            {
               session.StartDate,
               session.EndDate,
               session.StartTime,
               session.EndTime,
               session.Id
            };

            connection.Execute (@"UPDATE logs SET 
                                  StartDate = @StartDate, 
                                  EndDate   = @EndDate, 
                                  StartTime = @StartTime, 
                                  EndTime   = @EndTime
                                  WHERE Id = @Id",
                                  param);
         }
      });
   }

   public static void AddNewSession (CodingSession session, int projID)
   {
      Execute ((connection) =>
      {
         connection?.Execute (@"INSERT INTO logs (ProjectId, StartDate, EndDate, StartTime, EndTime)
                                VALUES (@ProjectId, @StartDate, @EndDate, @StartTime, @EndTime)",
                                session.GetAnonymousObject (projID));

         var data = connection?.Query ("SELECT MAX (Id) AS LastLog FROM logs")
                               .ToList ()
                               .FirstOrDefault ();

         if (data != null)
            session.Id = Convert.ToInt32 (data.LastLog);
      });
   }

   public static void DeleteProject (int projID)
   {
      Execute ((connection) =>
      {
         connection?.Execute ("DELETE FROM projects WHERE Id = @Id", new { Id = projID });
      });
   }

   public static void AddNewProject (string name)
   {
      Execute ((connection) =>
      {
         connection?.Execute ($"INSERT INTO projects (Name) VALUES (@Name)", new { Name = name });
      });
   }

   public static int GetProjectID (string projName)
   {
      int projID = 0;

      Execute ((connection) =>
      {
         var data = connection?.Query ("SELECT Id FROM projects WHERE Name = @Name", new { Name = projName })
                               .ToList ()
                               .FirstOrDefault ();

         if (data != null)
            projID = Convert.ToInt32 (data.Id);
      });

      return projID;
   }

   /// <summary>
   /// Returns name of project with most recent EndDate + EndTime
   /// </summary>
   /// <returns></returns>
   public static string GetNameOfMostRecentProject ()
   {
      string projName = string.Empty;

      Execute ((connection) =>
      {
         if (connection == null)
            return;

         string sql = @"SELECT Name FROM projects WHERE ID =
                       (SELECT ProjectId FROM logs ORDER BY CONCAT (EndDate, CAST (PRINTF('%04d', EndTime) AS TEXT (4))) DESC LIMIT 1)";
         var data = connection.Query (sql).ToList ().FirstOrDefault ();
         if (data != null)
            projName = data.Name;
      });

      return projName;
   }
}

using Dapper;
using Microsoft.Data.Sqlite;
using System.Xml.Linq;

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
      return EstablishConnection (SettingsHolder.Config.ConnectionString);
   }

   public static bool CreateEmptyDatabase ()
   {
      bool newDatabase = true;
      SqliteConnection connection = null;
      try
      {
         connection = EstablishDefaultConnection ();

         connection.Execute (@"CREATE TABLE IF NOT EXISTS projects
                               (Id INTEGER PRIMARY KEY AUTOINCREMENT,
                               Name TEXT UNIQUE NOT NULL)" );

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
      }
      catch (Exception)
      {
      }
      finally
      {
         connection?.Close ();
      }

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

      const int maxProjects = 5;
      const int maxLogsPerProject = 20;
      const int maxDurationInMinutes = 300; // 5 hours
      const int workPeriodInDays = 30;

      DateTime dtStart = DateTime.Now.AddDays (-workPeriodInDays);
      Random rand = new ();

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

         DateTime dt = dtStart.AddDays (rand.Next (0, workPeriodInDays));
         dt = dt.AddMinutes (rand.Next (0, 1440));

         int numLogs = rand.Next (1, maxLogsPerProject);

         var sessions = CodingSession.CreateDummySessions (dt, numLogs, maxDurationInMinutes);
         if (sessions == null || sessions.Count <= 0)
            continue;

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
         SettingsHolder.Config.LastProject = lastProject;
         SettingsHolder.Save ();
      }

      SqliteConnection connection = null;
      try
      {
         connection = EstablishDefaultConnection ();

         // Write all projects to db
         List<object> dataToWrite = new ();
         foreach (var (name, _) in allSessions)
         {
            CodingProject proj = new ()
            {
               Name = name
            };
            dataToWrite.Add (proj.GetAnonymousObject ());
         }

         connection.Execute ("INSERT INTO projects (Name) VALUES (@Name)", dataToWrite);
         List<CodingProject> projects = connection.Query<CodingProject> ("SELECT * FROM projects").ToList ();

         // Write sessions od all projects to db
         dataToWrite.Clear ();

         foreach (var proj in projects)
         {
            var sessions = allSessions [proj.Name];
            foreach (var session in sessions)
               dataToWrite.Add (session.GetAnonymousObject (proj.Id));
         }

         connection.Execute ("INSERT INTO logs (ProjectId, StartDate, EndDate, StartTime, EndTime) " +
                             "VALUES (@ProjectId, @StartDate, @EndDate, @StartTime, @EndTime)", 
                             dataToWrite);
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

   public static List<CodingProject> GetAllProjects ()
   {
      List<CodingProject> projects = null;
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         projects =  connection.Query<CodingProject> ("SELECT * FROM projects ORDER BY Name").ToList ();
      }
      catch (Exception ex)
      {
         throw;
      }
      finally 
      {
         connection?.Close ();
      }

      return projects;
   }

   public static List<CodingSession> GetAllSessionsOfProject (int projID)
   {
      List<CodingSession> sessions = null;
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         sessions = connection.Query<CodingSession> ($"SELECT * FROM logs WHERE ProjectId = {projID} ORDER BY StartDate, StartTime").ToList ();
      }
      catch (Exception)
      {
         throw;
      }
      finally
      {
         connection?.Close ();
      }

      return sessions;
   }

   public static void DeleteSession (int sessionID)
   {
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         connection.Execute ($"DELETE FROM logs WHERE Id = {sessionID}");
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

   public static void UpdateSessions (List<CodingSession> sessions)
   {
      if (sessions == null || sessions.Count <= 0)
         return;

      SqliteConnection connection = null;
      try
      {
         connection = EstablishDefaultConnection ();
         foreach (var session in sessions)
            connection.Execute ($"UPDATE logs SET StartDate = {session.StartDate}, EndDate = {session.EndDate}, " +
                                $"StartTime = {session.StartTime}, EndTime = {session.EndTime} WHERE Id = {session.Id}");
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

   public static void AddNewSession (CodingSession session, int projID)
   {
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         connection.Execute ("INSERT INTO logs (ProjectId, StartDate, EndDate, StartTime, EndTime) " +
                             "VALUES (@ProjectId, @StartDate, @EndDate, @StartTime, @EndTime)",
                             session.GetAnonymousObject (projID));
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

   public static void DeleteProject (int projID)
   {
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         connection.Execute ($"DELETE FROM projects WHERE Id = {projID}");
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

   public static void AddNewProject (string name)
   {
      SqliteConnection connection = null;

      try
      {
         connection = EstablishDefaultConnection ();
         connection.Execute ($"INSERT INTO projects (Name) VALUES ('{name}')");
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

   public static int GetProjectID (string projName)
   {
      SqliteConnection connection = null;
      int projID = 0;

      try
      {
         connection = EstablishDefaultConnection ();
         var data = connection.Query ("SELECT Id FROM projects WHERE Name = @Name", new { Name = projName})
                              .ToList ()
                              .FirstOrDefault ();

         if (data != null)
            projID = Convert.ToInt32 (data.Id);
      }
      catch (Exception)
      {
         throw;
      }
      finally
      {
         connection?.Close ();
      }

      return projID;
   }
}

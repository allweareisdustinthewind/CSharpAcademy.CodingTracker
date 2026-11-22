using CodingTracker.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CodingTracker.Filter;
using CodingTracker.UI;
using Spectre.Console;

namespace CodingTracker;

public class FilterExpr
{
   public string Text;
   
   public Expression Expr;

   public bool Active;

   public string ?SetFilterText (string text)
   {
      Interpreter interpreter = new ();
      Expression? expr = null;
      bool expectDateTime = IsExpectDateTime (text);

      try
      {
         expr = interpreter.Compile (text, expectDateTime);
         if (expr == null)
            return "Bad expression";

         if (expectDateTime)
            interpreter.Run (expr, "20000101", "00000");
         else
            interpreter.Run (expr, "00000");

         Text = text;
         Expr = expr;
      }
      catch (ParseException ex)
      {
         return ex.Message;
      }

      return null;
   }

   public bool Check (string date, int time)
   {
      if (Expr == null)
         return false;

      Interpreter interpreter = new ();
      return interpreter.Run (Expr, date, time.ToString ().PadLeft (5, '0'));
   }

   public bool Check (int val)
   {
      if (Expr == null)
         return false;

      Interpreter interpreter = new ();
      return interpreter.Run (Expr, val.ToString ().PadLeft (5, '0'));
   }

   bool IsExpectDateTime (string text)
   {
      return text.IndexOf ("date") >= 0 || text.IndexOf ("time") >= 0;
   }
}

public class Settings
{
   public string ConnectionString = "Data Source=log.db";
   public string ?LastProject; // Name of most recent project from last coding session

   public Dictionary<string, Dictionary<int, FilterExpr>>? Filter; // Configuration of filter per project and index of field in statisic's view

   public object GetAnonymousObject ()
   {
      Dictionary<string, Dictionary<int, string>> filters = null;
      if (Filter != null)
      {
         filters = new ();
         foreach (var (projName, filterData) in Filter)
         {
            Dictionary<int, string> data = new ();
            foreach (var (field, filterExpr) in filterData)
            {
               if (filterExpr == null || filterExpr.Expr == null || string.IsNullOrEmpty (filterExpr.Text))
                  continue;

               data.TryAdd (field, filterExpr.Text);
            }

            if (data.Count > 0)
               filters.TryAdd (projName, data);
         }
      }

      return new { ConnectionString, LastProject, Filter = filters };
   }
}

public static class SettingsHolder
{
   public readonly static string SettingsFile = "appsettings.json";
   public static Settings ?Config { get; set; }

   public static bool Load ()
   {
      Config = null;

      if (!File.Exists (SettingsFile))
         return false;

      Config = GetFromText (File.ReadAllText (SettingsFile));

      return Config != null;
   }

   public static Settings ?GetFromText (string json)
   {
      JObject obj = JObject.Parse (json);
      if (obj == null)
         return null;

      string [] mustFields = { "ConnectionString" };
      foreach (var field in mustFields)
      {
         if (!obj.ContainsKey (field))
            throw new Exception ($"The field '{field}' is not present in config file '{SettingsFile}'");
      }

      Settings settings = new ();
      string ?value = GetValueFromJson ("ConnectionString");
      if (value != null)
         settings.ConnectionString = value;

      value = GetValueFromJson ("LastProject");
      if (value != null)
         settings.LastProject = value;

      List<string> errors = new ();

      if (obj.ContainsKey ("Filter"))
      {
         var objFilter = obj ["Filter"];
         if (objFilter != null)
         {
            var filters = objFilter.ToObject<Dictionary<string, Dictionary<int, string>>> ();
            if (filters != null && filters.Count > 0)
            {
               settings.Filter = new ();
               foreach (var (projName, projFilters) in filters)
               {
                  Dictionary<int, FilterExpr> data = new ();
                  foreach (var (column, filter) in projFilters)
                  {
                     FilterExpr expr = new ();
                     string ?message = expr.SetFilterText (filter);
                     if (!string.IsNullOrEmpty (message))
                     {
                        errors.Add ($"Filter of project {projName} column {column} is malformed: {message}");
                        continue;
                     }

                     data.TryAdd (column, expr);
                  }

                  if (data.Count > 0)
                     settings.Filter.TryAdd (projName, data);
               }
            }
         }
      }

      if (errors.Count > 0 )
      {
         using HideCursor hider = new ();

         foreach (var text in errors)
            AnsiConsole.MarkupLine ($"[white on red] {text} [/]");

         AnsiConsole.MarkupLine ($"[yellow]Press any key to continue...[/]");
         Console.ReadKey (true);
      }

      string ?GetValueFromJson (string name)
      {
         if (!obj.ContainsKey (name))
            return null;

         var value = obj [name];
         if (value == null)
            return null;

         return value.ToString ();
      }

      return settings;
   }


   public static void CreateDefaultConfig ()
   {
      Config = new ();
      Save ();
   }

   public static void Save ()
   {
      if (Config == null)
         return;

      JsonSerializer serializer = new ()
      {
         Formatting = Formatting.Indented
      };

      using (StreamWriter writer = new (SettingsFile))
      using (JsonWriter jsWriter = new JsonTextWriter (writer))
      {
         serializer.Serialize (jsWriter, Config.GetAnonymousObject ());
      }
   }

   public static void DeleteProject (string projName)
   {
      if (Config == null || string.IsNullOrEmpty (projName))
         return;

      bool isChanged = false;
      if (Config.LastProject == projName)
      {
         Config.LastProject = DbManager.GetNameOfMostRecentProject ();
         isChanged = true;
      }

      if (Config.Filter != null && Config.Filter.ContainsKey (projName))
      {
         Config.Filter.Remove (projName);
         isChanged = true;
      }

      if (isChanged)
         Save ();
   }

   public static FilterExpr ?GetFilter (string projName, int fieldIndex)
   {
      if (Config == null || Config.Filter == null)
         return null;

      if (!Config.Filter.TryGetValue (projName, out var filters) || filters == null)
         return null;

      if (!filters.TryGetValue (fieldIndex, out var filterExpr))
         return null;

      return filterExpr;
   }

   public static void SaveFilter (string projName, int fieldIndex, FilterExpr filter)
   {
      if (Config == null || string.IsNullOrEmpty (projName) || filter == null || filter.Expr == null)
         return;

      if (Config.Filter == null)
         Config.Filter = new ();

      var allFilters = Config.Filter;
      Dictionary<int, FilterExpr> ?filters = null;
      if (!allFilters.TryGetValue (projName, out filters))
      {
         filters = new ();
         allFilters.Add (projName, filters);
      }

      filters [fieldIndex] = filter;

      Save ();
   }
}
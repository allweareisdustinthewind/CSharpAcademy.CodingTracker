using CodingTracker.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodingTracker;

public class Settings
{
   public string ConnectionString = "Data Source=log.db";
   public string ?LastProject; // Name of most recent project from last coding session

   public Dictionary<string, Dictionary<int, string>>? Filter; // Configuration of filter per project and index of field in statisic's view
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

      return obj.ToObject<Settings> ();
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
         serializer.Serialize (jsWriter, Config);
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

   public static void SaveFilter (string projName, int fieldIndex, string config)
   {
      if (Config == null || string.IsNullOrEmpty (projName) || string.IsNullOrEmpty (config))
         return;

      if (Config.Filter == null)
         Config.Filter = new ();

      var allFilters = Config.Filter;
      Dictionary<int, string> ?filters = null;
      if (!allFilters.TryGetValue (projName, out filters))
      {
         filters = new ();
         allFilters.Add (projName, filters);
      }

      filters [fieldIndex] = config;

      Save ();
   }
}
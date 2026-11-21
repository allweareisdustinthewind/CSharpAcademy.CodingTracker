namespace CodingTracker.UnitTests;

public class SettingsTests
{
   [TestCase ("")]
   [TestCase ("{}")]
   [TestCase ("{\"LastProject\": null}")]
   public void LoadBadConfig_ThrowsError (string json)
   {
      Assert.That (() => SettingsHolder.GetFromText (json), Throws.Exception);
   }

   [TestCase ("{\"ConnectionString\": \"SomeData\",\"LastProject\": null}")]
   [TestCase ("{\"ConnectionString\": \"SomeData\"}")]
   public void CorrectConfig_ParsedSuccessfully (string json)
   {
      Assert.That (() => SettingsHolder.GetFromText (json), Throws.Nothing);
   }
}
using CodingTracker.Database;

namespace CodingTracker.UnitTests;

public class DbTests
{
   [TestCase ("Data Source=ZZ:\\log.db")]
   [TestCase ("blablabla")]
   public void TryToConnectWithBadDataSource_ThrowsError (string connectionString)
   {
      Assert.That (() => DbManager.EstablishConnection (connectionString), Throws.Exception);
   }
}
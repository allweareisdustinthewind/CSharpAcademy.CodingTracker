
namespace CodingTracker;

public class CodingProject
{
   public int Id { get; set; }
   public string Name { get; set; }

   public object GetAnonymousObject ()
   {
      return new { Name };
   }
}

using CodingTracker.Filter.Lexical;

namespace CodingTracker.Filter;

internal class FilterExpr
{
   public bool Evaluate (string expr, string date, string time)
   {
      return EvaluateIntern (expr, date, time);
   }

   public bool Evaluate (string expr, string duration)
   {
      return EvaluateIntern (expr, duration, string.Empty);
   }

   bool EvaluateIntern (string expr, string date, string time)
   {
      Scanner scanner = new (expr.ToLower ());
      var tokens = scanner.DoScan ();
      if (tokens?.Count <= 0)
         return false;

      Parser parser = new (tokens, date, time);
      parser.parse ();

      return true;
   }
}

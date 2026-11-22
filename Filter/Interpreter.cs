namespace CodingTracker.Filter;

public class Interpreter : IVisitor
{
   string _date;
   string _time;
   string _duration;

   public string visitBinaryExp (BinaryExpr expr)
   {
      string valLeft  = Evaluate (expr.ExprLeft);
      string valRight = Evaluate (expr.ExprRight);

      string res = expr.Operation.type switch
      {
         TokenType.And => valLeft == "false" || valRight == "false"    ? "false" : "true",
         TokenType.Or => valLeft == "true" || valRight == "true"       ? "true" : "false",
         TokenType.Greater => valLeft.CompareTo (valRight) > 0         ? "true" : "false",
         TokenType.GreaterOrEqual => valLeft.CompareTo (valRight) >= 0 ? "true" : "false",
         TokenType.Less => valLeft.CompareTo (valRight) < 0            ? "true" : "false",
         TokenType.LessOrEqual => valLeft.CompareTo (valRight) <= 0    ? "true" : "false",
         TokenType.Equal => valLeft == valRight                        ? "true" : "false",
         TokenType.NotEqual => valLeft != valRight                     ? "true" : "false",
         _ => "false"
      };

      return res;
   }

   public string visitGroupingExp (GroupingExpr expr)
   {
      return Evaluate (expr.Expr);
   }

   public string visitLiteralExp (LiteralExpr expr)
   {
      string res = expr.Value switch
      {
         "date" => _date,
         "time" => _time,
         "val"  => _duration,
         _      => expr.Value
      };
      return res;
   }

   public string visitUnaryExp (UnaryExpr expr)
   {
      string res = Evaluate (expr.Expr);
      if (expr.Operation.type == TokenType.Not)
         return res == "true" ? "false" : "true";

      return res;
   }

   string Evaluate (Expression expr)
   {
      return expr.accept (this);
   }

   public Expression? Compile (string filterText, bool expectDateTime)
   {
      try
      {
         Scanner scanner = new (filterText.ToLower ());
         var tokens = scanner.DoScan ();
         if (tokens?.Count <= 0)
            throw new ParseException (0, "in syntax", $"The expression '{filterText}' is not valid");

         bool isDate = false;
         bool isTime = false;
         bool isVal = false;
         foreach (var token in tokens)
         {
            if (token.type == TokenType.Date)
               isDate = true;
            else if (token.type == TokenType.Time)
               isTime = true;
            else if (token.type == TokenType.Val)
               isVal = true;
         }

         if (isVal)
         { 
            if (isDate)
               throw new ParseException (0, "in syntax", $"Using 'date' and 'val' in same expression is not allowed");
            else
               if (isTime)
                  throw new ParseException (0, "in syntax", $"Using 'time' and 'val' in same expression is not allowed");
         }
         else if (!isDate && !isTime)
            throw new ParseException (0, "in syntax", $"Expression is invalid: it contains no 'date', 'time' or 'val'.");


         return new Parser (tokens, expectDateTime).parse ();
      }
      catch (ParseException ex)
      {
         throw;
      }
   }

   public bool Run (Expression ?expr, string date, string time)
   {
      if (expr == null)
         throw new ParseException (0, "in syntax", "Error in text of expression");

      _date = date;
      _time = time.PadLeft (5, '0');

      return RunIntern (expr);
   }

   public bool Run (Expression? expr, string duration)
   {

      if (expr == null)
         throw new ParseException (0, "in syntax", "Error in text of expression");

      _duration = duration.PadLeft (10, '0');

      return RunIntern (expr);
   }

   bool RunIntern (Expression? expr)
   {
      return Evaluate (expr) == "true";
   }
}

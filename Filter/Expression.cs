namespace CodingTracker.Filter;

internal abstract class Expression
{
   abstract public string accept (IVisitor visitor);
}

internal interface IVisitor
{
   string visitBinaryExp (BinaryExpr expr);
   string visitGroupingExp (GroupingExpr expr);
   string visitLiteralExp (LiteralExpr expr);
   string visitUnaryExp (UnaryExpr expr);
}

internal class BinaryExpr : Expression
{
   public Expression ExprLeft;
   public Token Operation;
   public Expression ExprRight;

   public BinaryExpr (Expression left, Token oper, Expression right)
   {
      ExprLeft = left;
      Operation = oper;
      ExprRight = right;
   }

   public void PrepareDataToCompare (TokenType type)
   {
      if (ExprRight is not LiteralExpr)
         return;

      LiteralExpr expr = (LiteralExpr) ExprRight;
      expr.Value = type switch
      {
         TokenType.Date => PrepareDate (expr.Value),
         TokenType.Time => PrepareTime (expr.Value),
         TokenType.Val => PrepareVal (expr.Value),
         _ => expr.Value
      };
   }

   string PrepareDate (string date)
   {
      if (date != "now" && !Checker.IsDateValid (date))
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Date is in wrong format");

      return Converter.GetDateFromString (date);
   }

   string PrepareTime (string time)
   {
      if (time != "now" && !Checker.IsTimeValid (time))
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Time is in wrong format");

      return Converter.GetTimeFromString (time).ToString ();
   }

   string PrepareVal (string val)
   {
      if (val == "now")
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Duration in format 'now' is not accepted");

      if (!Checker.IsDurationValid (val))
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Duration is in wrong format");

      return Converter.GetDurationFromString (val).ToString ();
   }

   public override string accept (IVisitor visitor)
   {
      return visitor.visitBinaryExp (this);
   }
}

internal class GroupingExpr : Expression
{
   public Expression Expr;

   public GroupingExpr (Expression expr)
   {
      Expr = expr;
   }

   public override string accept (IVisitor visitor)
   {
      return visitor.visitGroupingExp (this);
   }
}

internal class LiteralExpr : Expression
{
   public string Value;

   public LiteralExpr (string val)
   {
      Value = val;
   }

   public override string accept (IVisitor visitor)
   {
      return visitor.visitLiteralExp (this);
   }
}

internal class UnaryExpr : Expression
{
   public Token Operation;
   public Expression Expr;

   public UnaryExpr (Token oper, Expression exp)
   {
      Operation = oper;
      Expr = exp;
   }

   public override string accept (IVisitor visitor)
   {
      return visitor.visitUnaryExp (this);
   }
}
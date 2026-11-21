namespace CodingTracker.Filter;

public abstract class Expression
{
   abstract public string accept (IVisitor visitor);
}

public interface IVisitor
{
   string visitBinaryExp (BinaryExpr expr);
   string visitGroupingExp (GroupingExpr expr);
   string visitLiteralExp (LiteralExpr expr);
   string visitUnaryExp (UnaryExpr expr);
}

public class BinaryExpr : Expression
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
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Date is in wrong format, expects dd:mm:yyyy");

      return Converter.GetDateFromString (date);
   }

   string PrepareTime (string time)
   {
      if (time != "now" && !Checker.IsTimeValid (time))
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Time is in wrong format, expects hh:mm:ss");

      return Converter.GetTimeFromString (time).ToString ();
   }

   string PrepareVal (string val)
   {
      if (val == "now")
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Duration in format 'now' is not accepted");

      if (!Checker.IsDurationValid (val))
         throw new ParseException (Operation.position, $"at {Operation.lexeme}", "Duration is in wrong format, expects hh:mm:ss or 'N1'h 'N2'm 'N3's");

      return Converter.GetDurationFromString (val).ToString ();
   }

   public override string accept (IVisitor visitor)
   {
      return visitor.visitBinaryExp (this);
   }
}

public class GroupingExpr : Expression
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

public class LiteralExpr : Expression
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

public class UnaryExpr : Expression
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
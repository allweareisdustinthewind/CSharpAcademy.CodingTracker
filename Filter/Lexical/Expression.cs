
namespace CodingTracker.Filter.Lexical;

internal abstract class Expression
{
   abstract public bool accept (IVisitor visitor);
}

internal interface IVisitor
{
   bool visitBinaryExp (BinaryExp exp);
   bool visitGroupingExp (GroupingExp exp);
   bool visitLiteralExp (LiteralExp exp);
   bool visitUnaryExp (UnaryExp exp);
}

internal class BinaryExp : Expression
{
   Expression _expLeft;
   Token _oper;
   Expression _expRight;

   public BinaryExp (Expression left, Token oper, Expression right)
   {
      _expLeft = left;
      _oper = oper;
      _expRight = right;
   }

   public override bool accept (IVisitor visitor)
   {
      return visitor.visitBinaryExp (this);
   }
}

internal class GroupingExp : Expression
{
   Expression _exp;

   public GroupingExp (Expression exp)
   {
      _exp = exp;
   }

   public override bool accept (IVisitor visitor)
   {
      return visitor.visitGroupingExp (this);
   }
}

internal class LiteralExp : Expression
{
   string _val;

   public LiteralExp (string val)
   {
      _val = val;
   }

   public override bool accept (IVisitor visitor)
   {
      return visitor.visitLiteralExp (this);
   }
}

internal class UnaryExp : Expression
{
   Token _oper;
   Expression _exp;

   public UnaryExp (Token oper, Expression exp)
   {
      _oper = oper;
      _exp = exp;
   }

   public override bool accept (IVisitor visitor)
   {
      return visitor.visitUnaryExp (this);
   }
}
namespace CodingTracker.Filter.Lexical;

internal class Interpreter : IVisitor
{
   public bool visitBinaryExp (BinaryExp exp)
   {
      return true;
   }

   public bool visitGroupingExp (GroupingExp exp)
   {
      return true;
   }

   public bool visitLiteralExp (LiteralExp exp)
   {
      return true;
   }

   public bool visitUnaryExp (UnaryExp exp)
   {
      return true;
   }
}

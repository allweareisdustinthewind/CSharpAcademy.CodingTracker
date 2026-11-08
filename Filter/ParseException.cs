namespace CodingTracker.Filter;

internal class ParseException : Exception
{
   public ParseException (int pos, string posDesc, string message)
      : base ($"Error {posDesc}, Column {pos}: {message}")
   {
   }

   public static ParseException report (Token token, string message)
   {
      if (token.type == TokenType.End)
         return new ParseException (token.position, "at end", message);

      return new ParseException (token.position, $"at '{token.lexeme}'", message);
   }
}

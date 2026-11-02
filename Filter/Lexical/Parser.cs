namespace CodingTracker.Filter.Lexical;

//- expression → equality
//- equality → comparison (("=" | "<>") comparison)*
//- comparison → unary (("<" | "<=" | ">" | ">=") unary)*
//- unary → "not" | unary | primary
//- primary → STRING | "now" | "(" expression ")"

internal class Parser
{
   List<Token> _tokens;
   int _curIndex;
   string _date;
   string _time;
   string _duration;

   public Parser (List<Token> tokens, string date, string time)
   {
      _tokens = tokens;

      if (string.IsNullOrEmpty (time))
         _duration = date;
      else
      {
         _date = date;
         _time = time;
      }
   }

   public Expression parse ()
   {
      try
      {
         return expression ();
      }
      catch (ParseException ex)
      {
         return null;
      }
   }

   Expression expression ()
   {
      return equality ();
   }

   Expression equality ()
   {
      Expression expr = comparison ();

      while (match (TokenType.Equal, TokenType.NotEqual))
      {
         Token op = previous ();
         Expression exprRight = comparison ();
         expr = new BinaryExp (expr, op, exprRight);
      }

      return expr;
   }

   Expression comparison ()
   {
      Expression expr = unary ();

      while (match (TokenType.Less, TokenType.LessOrEqual, TokenType.Greater, TokenType.GreaterOrEqual))
      {
         Token op = previous ();
         Expression exprRight = unary ();
         expr = new BinaryExp (expr, op, exprRight);
      }

      return expr;
   }

   Expression unary ()
   {
      if (match (TokenType.Not))
      {
         Token op = previous ();
         Expression expr = unary ();
         return new UnaryExp (op, expr); 
      }

      return primary ();
   }

   Expression primary ()
   {
      if (match (TokenType.Date))
         return new LiteralExp (_date);

      if (match (TokenType.Time))
         return new LiteralExp (_time);

      if (match (TokenType.Val))
      {
         if (string.IsNullOrEmpty (_time))
            throw ParseException.report (peek (), "Parameter 'val' is not allowed");

         return new LiteralExp (_duration);
      }

      if (match (TokenType.And))
         return new LiteralExp ("and");

      if (match (TokenType.Or))
         return new LiteralExp ("or");

      if (match (TokenType.Text))
         return new LiteralExp (previous ().literal);

      if (match (TokenType.LeftBrace))
      {
         Expression expr = expression ();
         consume (TokenType.RightBrace, "Expect ')' after expression.");
         return new GroupingExp (expr);
      }

      throw ParseException.report (peek (), "Expect expression");
   }

   bool match (params TokenType [] types)
   {
      foreach (var type in types)
      {
         if (check (type))
         {
            advance ();
            return true;
         }
      }

      return false;
   }

   bool check (TokenType type)
   {
      if (isAtEnd ())
         return false;

      return peek ().type == type;
   }

   Token advance ()
   {
      if (!isAtEnd ())
         ++_curIndex;

      return previous ();
   }

   bool isAtEnd ()
   {
      return peek ().type == TokenType.End;
   }

   Token peek ()
   {
      return _tokens [_curIndex];
   }

   Token previous ()
   {
      return _tokens [_curIndex - 1];
   }

   Token consume (TokenType type, string message)
   {
      if (check (type))
         return advance ();

      throw ParseException.report (peek (), message);
   }
}

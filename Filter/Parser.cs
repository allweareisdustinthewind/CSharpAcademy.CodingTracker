namespace CodingTracker.Filter;

internal class Parser
{
   List<Token> _tokens;
   int _curIndex;
   bool _expectDateTime; // If true, then are the input parameters 'date' and 'time' available.
                         // Otherwise one can use only the parameter 'val' (duration)

   public Parser (List<Token> tokens, bool expectDateTime)
   {
      _tokens = tokens;
      _expectDateTime = expectDateTime;
   }

   public Expression parse ()
   {
      Expression expr = expression ();
      if (!isAtEnd ())
         throw ParseException.report (peek (), "Expression contains unrecognised character");

      return expr;
   }

   Expression expression ()
   {
      Expression expr = equality ();

      while (match (TokenType.And, TokenType.Or))
      {
         Token op = previous ();
         Expression exprRight = equality ();
         expr = new BinaryExpr (expr, op, exprRight);
      }

      return expr;
   }

   Expression equality ()
   {
      Expression expr = comparison ();

      while (match (TokenType.Equal, TokenType.NotEqual))
      {
         Token op = previous ();
         Token op_prev = before_previous ();

         Expression exprRight = comparison ();
         BinaryExpr exprBin = new (expr, op, exprRight);
         if (op_prev.type == TokenType.Date || op_prev.type == TokenType.Time || op_prev.type == TokenType.Val)
            exprBin.PrepareDataToCompare (op_prev.type);

         expr = exprBin;
      }

      return expr;
   }

   Expression comparison ()
   {
      Expression expr = unary ();

      while (match (TokenType.Less, TokenType.LessOrEqual, TokenType.Greater, TokenType.GreaterOrEqual))
      {
         Token op = previous ();
         Token op_prev = before_previous ();

         Expression exprRight = unary ();
         BinaryExpr exprBin = new (expr, op, exprRight);
         if (op_prev.type == TokenType.Date || op_prev.type == TokenType.Time || op_prev.type == TokenType.Val)
            exprBin.PrepareDataToCompare (op_prev.type);

         expr = exprBin;
      }

      return expr;
   }

   Expression unary ()
   {
      if (match (TokenType.Not))
      {
         Token op = previous ();
         Expression expr = unary ();
         return new UnaryExpr (op, expr); 
      }

      return primary ();
   }

   Expression primary ()
   {
      if (match (TokenType.Date))
      {
         if (!_expectDateTime)
            throw ParseException.report (peek (), "Parameter 'date' is not allowed");

         return new LiteralExpr ("date");
      }

      if (match (TokenType.Time))
      {
         if (!_expectDateTime)
            throw ParseException.report (peek (), "Parameter 'time' is not allowed");

         return new LiteralExpr ("time");
      }

      if (match (TokenType.Val))
      {
         if (_expectDateTime)
            throw ParseException.report (peek (), "Parameter 'val' is not allowed");

         return new LiteralExpr ("val");
      }

      if (match (TokenType.And))
         return new LiteralExpr ("and");

      if (match (TokenType.Or))
         return new LiteralExpr ("or");

      if (match (TokenType.Text))
         return new LiteralExpr (previous ().literal);

      if (match (TokenType.LeftBrace))
      {
         Expression expr = expression ();
         consume (TokenType.RightBrace, "Expect ')' after expression.");
         return new GroupingExpr (expr);
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

   Token before_previous ()
   {
      return _tokens [_curIndex - 2];
   }

   Token consume (TokenType type, string message)
   {
      if (check (type))
         return advance ();

      throw ParseException.report (peek (), message);
   }
}

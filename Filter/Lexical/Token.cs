namespace CodingTracker.Filter.Lexical;

internal enum TokenType
{
   LeftBrace,
   RightBrace,
   Equal,
   NotEqual,
   Less,
   Greater,
   LessOrEqual,
   GreaterOrEqual,
   Text,
   And,
   Or,
   Not,
   Date,
   Time,
   Val,
   End
};

internal class Token
{
   public TokenType type;
   public string lexeme;

   public string? literal;
   public int position;

   public Token (TokenType _type, string _lexeme, string? _literal, int _position)
   {
      type = _type;
      lexeme = _lexeme;
      literal = _literal;
      position = _position;
   }
}

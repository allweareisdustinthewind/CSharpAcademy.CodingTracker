namespace CodingTracker.Filter;

internal class Scanner
{
   string _source;
   int _startPos;
   int _currentPos = 0;
   List<Token> _tokens = new ();
   Dictionary<string, TokenType> _keywords = new ()
   {
      { "and", TokenType.And },
      { "or",  TokenType.Or },
      { "not", TokenType.Not },
      { "date", TokenType.Date },
      { "time", TokenType.Time },
      { "val", TokenType.Val }
   };

   public string LastError;

   public Scanner (string source)
   {
      _source = source;
   }

   public List<Token> DoScan ()
   {
      if (string.IsNullOrEmpty (_source))
         return new ();

      while (!IsEndOfString ())
      {
         _startPos = _currentPos;
         ScanOneToken ();
      }

      _tokens.Add (new (TokenType.End, string.Empty, null, _source.Length));
      return _tokens;
   }

   bool IsEndOfString ()
   {
      return _currentPos >= _source.Length;
   }

   void ScanOneToken ()
   {
      char ch = GetNextSymbol ();

      switch (ch)
      {
         case '(':
            AddToken (TokenType.LeftBrace);
            break;

         case ')':
            AddToken (TokenType.RightBrace);
            break;

         case '=':
            AddToken (TokenType.Equal);
            break;

         case '<':
            AddToken (Match ('=') ? TokenType.LessOrEqual : Match ('>') ? TokenType.NotEqual : TokenType.Less);
            break;

         case '>':
            AddToken (Match ('=') ? TokenType.GreaterOrEqual : TokenType.Greater);
            break;

         case ' ':
         case '\r':
         case '\n':
         case '\t':
            break;

         case '\'':
            ReadText ();
            break;

         default:
            if (IsAlpha (ch))
               ReadKeyword ();
            else
               ReportError ("Unknown character");
            break;
      }
   }

   char GetNextSymbol ()
   {
      return _source [_currentPos++];
   }

   void AddToken (TokenType type)
   {
      AddToken (type, null);
   }

   void AddToken (TokenType type, string? literal)
   {
      string lexeme = _source.Substring (_startPos, _currentPos - _startPos);
      _tokens.Add (new (type, lexeme, literal, _currentPos));
   }

   void ReadText ()
   {
      while (Peek () != '\'' && !IsEndOfString ())
         GetNextSymbol ();

      if (IsEndOfString ())
      {
         ReportError ("Unterminated string");
         return;
      }

      GetNextSymbol ();

      int len = _currentPos - _startPos - 2;
      string text = _source.Substring (_startPos + 1, len);
      AddToken (TokenType.Text, text);
   }

   void ReadKeyword ()
   {
      while (IsAlpha (Peek ()))
         GetNextSymbol ();

      int len = _currentPos - _startPos;
      string text = _source.Substring (_startPos, len);
      if (!_keywords.TryGetValue (text, out var type))
      {
         ReportError ("Unknown reserved word");
         return;
      }

      AddToken (type);
   }

   bool Match (char expected)
   {
      if (IsEndOfString ())
         return false;

      if (_source [_currentPos] != expected)
         return false;

      ++_currentPos;
      return true;
   }

   char Peek ()
   {
      if (IsEndOfString ())
         return '\0';

      return _source [_currentPos];
   }

   bool IsAlpha (char ch)
   {
      // Input character is always in lower case
      return ch >= 'a' && ch <= 'z';
   }

   void ReportError (string message)
   {
      LastError += $"Syntax error in column {_currentPos + 1}: {message}\n";
   }
}

namespace CodingTracker.UnitTests;

using CodingTracker.Filter;

internal class FilterTests
{
   static readonly object [] BadSyntax =
   [
      new TestCaseData ("date < 123"),
      new TestCaseData ("date > '01.01.2026' && date < '02.02.2026'"),
      new TestCaseData ("val > '12'"),
      new TestCaseData ("date == '01.01.2026'"),
      new TestCaseData ("(date < '01.01.2026'))"),
      new TestCaseData ("'01.01.2026' = '01.01.2026'"),
      new TestCaseData ("date1 = '01.01.2026'"),
      new TestCaseData ("date_ = '01.01.2026'"),
      new TestCaseData ("&date = '01.01.2026'"),
      new TestCaseData ("date '01.01.2026'"),
      new TestCaseData ("val < 1h")
   ];

   static readonly object [] GoodSyntax =
   [
      new TestCaseData ("date <= '01.01.2026'"),
      new TestCaseData ("date > '01.01.2026' and date < '02.02.2026'"),
      new TestCaseData ("date = '01.01.2026'"),
      new TestCaseData ("(date < '01.01.2026') or (time <> '02:13:20')")
   ];

   static readonly object [] GoodSyntaxWithValues =
   [
      new TestCaseData ("date <= '01.01.2026'", "20251010", "00:00", true),
      new TestCaseData ("date > '01.01.2026' and date < '02.02.2026'", "20260105", "1122", true),
      new TestCaseData ("date = '01.01.2026'", "20260201", "00000", false),
      new TestCaseData ("(date < '01.01.2026') and (time <> '02:13:20')", "20260808", "8000", false)
   ];
   
   [TestCaseSource(nameof (BadSyntax))]
   public void BadSyntaxOfFilterExpression_ThrowsException (string expr)
   {
      Assert.That (() => (new Interpreter ()).Compile (expr, expectDateTime: true), Throws.Exception);
   }

   [TestCaseSource (nameof (GoodSyntax))]
   public void GoodSyntaxOfFilterExpression_NotThrowsException (string expr)
   {
      Interpreter interpreter = new ();
      Assert.That (() => interpreter.Compile (expr, expectDateTime: true), Throws.Nothing);

      var compiledExpr = interpreter.Compile (expr, expectDateTime: true);
      Assert.That (compiledExpr, Is.Not.EqualTo (null));
   }

   [TestCaseSource (nameof (GoodSyntaxWithValues))]
   public void GoodSyntaxOfFilterExpressionWithValues_ReturnsCorrectResult (string expr, string data, string time, bool expectedRes)
   {
      Interpreter interpreter = new ();
      var compiledExpr = interpreter.Compile (expr, expectDateTime: true);
      Assert.That (compiledExpr, Is.Not.EqualTo (null));

      if (compiledExpr == null)
         return;

      bool res = interpreter.Run (compiledExpr, data, time);
      Assert.That (res, Is.EqualTo (expectedRes));
   }
}

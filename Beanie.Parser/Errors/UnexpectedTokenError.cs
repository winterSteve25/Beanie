using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedTokenError(Token Token, params TokenType[] Expected) : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report($"Expected one of: '{ExpectedString()}' But found: {Token}", Token.Line, Token.Start, Token.End);
    }

    private string ExpectedString()
    {
        return string.Join(',', Expected);
    }

    public void Deconstruct(out Token Token, out TokenType[] Expected)
    {
        Token = this.Token;
        Expected = this.Expected;
    }
}
using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedTokenError : IError
{
    public UnexpectedTokenError(Token Token, params TokenType[] Expected)
    {
        this.Token = Token;
        this.Expected = Expected;
    }

    public void Report(IErrorReporter reporter)
    {
        reporter.Report($"Expected one of: '{ExpectedString()}' But found: {Token}", Token.Line, Token.Start, Token.End);
    }

    private string ExpectedString()
    {
        return string.Join(',', Expected);
    }

    public Token Token { get; init; }
    public TokenType[] Expected { get; init; }

    public void Deconstruct(out Token Token, out TokenType[] Expected)
    {
        Token = this.Token;
        Expected = this.Expected;
    }
}
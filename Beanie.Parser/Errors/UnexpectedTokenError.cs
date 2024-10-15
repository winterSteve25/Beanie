using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedTokenError(Token Token, TokenType[] Expected, string? Msg) : IError
{
    public void Report(IErrorReporter reporter)
    {
        if (Msg is null)
        {
            reporter.Report($"Expected one of: '{Expected}', But found: {Token}", Token.Line, Token.Start, Token.End);
            return;
        }

        reporter.Report($"{Msg}, Expected one of: '{Expected}', But found: {Token}", Token.Line, Token.Start, Token.End);
    }

    private string ExpectedString()
    {
        return string.Join(',', Expected);
    }
}
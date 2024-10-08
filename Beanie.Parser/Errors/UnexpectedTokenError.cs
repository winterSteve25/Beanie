using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedTokenError(Token Token) : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report($"Unexpected Token: {Token.TokenData}", Token.Line, Token.Start, Token.End);
    }
}
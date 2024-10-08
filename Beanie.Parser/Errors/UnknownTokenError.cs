using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnknownTokenError(Token Token) : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report($"Unknown token: {Token.TokenData}", Token.Line, Token.Start, Token.End);
    }
}
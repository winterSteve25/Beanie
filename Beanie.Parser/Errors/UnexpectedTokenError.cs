using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedTokenError(Token Token, TokenType expected, string msg) : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report($"{msg}, Expected: {expected}, But found: {Token}", Token.Line, Token.Start, Token.End);
    }
}
using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedEofError() : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report("Unexpected end of file", reporter.LineCount, reporter.Source.Length, reporter.Source.Length);
    }
}
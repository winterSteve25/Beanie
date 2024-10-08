using ErrFmt;

namespace Parser.Errors;

public readonly record struct UnexpectedEofError(int Line) : IError
{
    public void Report(IErrorReporter reporter)
    {
        reporter.Report("Unexpected end of file", Line, reporter.Source.Length, reporter.Source.Length);
    }
}
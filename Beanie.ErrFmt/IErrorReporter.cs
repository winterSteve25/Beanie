namespace ErrFmt;

public interface IErrorReporter
{
    string Report(string message, int line, int start, int end);
    
    string Source { get; }
    
    int LineCount { get; }
}
namespace ErrFmt;

public interface IErrorFormatter
{
    string Format(string message, int line, int start, int end);
}
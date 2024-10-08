namespace ErrFmt;

public interface IError
{
    void Report(IErrorReporter reporter);
}
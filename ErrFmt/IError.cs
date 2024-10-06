namespace ErrFmt;

public interface IError
{
    string Display(IErrorFormatter formatter);
}
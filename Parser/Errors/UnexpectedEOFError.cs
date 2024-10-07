using ErrFmt;

namespace Parser.Errors;

public record struct UnexpectedEofError(int Line) : IError
{
    public string Display(IErrorFormatter formatter)
    {
        return formatter.Format("Unexpected end of file", Line, formatter.Source.Length, formatter.Source.Length);
    }
}
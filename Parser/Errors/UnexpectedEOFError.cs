using ErrFmt;

namespace Parser.Errors;

public record struct UnexpectedEofError(int Loc, int Line) : IError
{
    public string Display(IErrorFormatter formatter)
    {
        return formatter.Format("Unexpected end of file", Line, Loc, Loc);
    }
}
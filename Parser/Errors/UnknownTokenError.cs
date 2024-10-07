using ErrFmt;

namespace Parser.Errors;

public record struct UnknownTokenError(Token Token) : IError
{
    public string Display(IErrorFormatter formatter)
    {
        return formatter.Format($"Unknown token: {Token.TokenData}", Token.Line, Token.Start, Token.End);
    }
}
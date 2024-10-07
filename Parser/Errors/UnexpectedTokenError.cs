using ErrFmt;

namespace Parser.Errors;

public record struct UnexpectedTokenError(Token Token) : IError
{
    public string Display(IErrorFormatter formatter)
    {
        return formatter.Format($"Unexpected Token: {Token.TokenData}", Token.Line, Token.Start, Token.End);
    }
}
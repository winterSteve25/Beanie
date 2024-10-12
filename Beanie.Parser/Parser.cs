using ErrFmt;
using Parser.Errors;

namespace Parser;

public class Parser
{
    private readonly List<Token> _tokens;
    private readonly List<IError> _errs;
    
    private int _current = 0;

    public Parser(List<Token> tokens, List<IError> errs)
    {
        _tokens = tokens;
        _errs = errs;
    }

    private Token Peek() => _tokens[_current];
    private Token Previous() => _tokens[_current - 1];
    private bool IsAtEnd() => _current >= _tokens.Count;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool Check(TokenType type)
    {
        if (IsAtEnd()) return false;
        return Peek().Type == type;
    }

    private bool Match(params TokenType[] types)
    {
        if (!types.Any(Check)) return false;
        Advance();
        return true;
    }

    private Token? Consume(TokenType type, string errorMessage)
    {
        if (Check(type)) return Advance();
        _errs.Add(new UnexpectedTokenError(Peek(), type, errorMessage));
        return null;
    }
}
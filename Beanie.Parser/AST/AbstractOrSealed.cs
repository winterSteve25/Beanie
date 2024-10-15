namespace Parser.AST;

public record AbstractOrSealed(Token Token, int Start, int End) : IAstElement
{
    public bool IsAbstract()
    {
        return Token.Type == TokenType.Abstract;
    }
    
    public bool IsSealed()
    {
        return Token.Type == TokenType.Sealed;
    }
}
namespace Parser.AST;

public record Identifier(Token Ident) : IExpression
{
    public int Start => Ident.Start;
    public int End => Ident.End;
}
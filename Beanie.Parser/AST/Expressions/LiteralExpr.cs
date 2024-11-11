namespace Parser.AST.Expressions;

public record LiteralExpr(Token LiteralToken) : IExpression
{
    public int Start => LiteralToken.Start;
    public int End => LiteralToken.End;
}
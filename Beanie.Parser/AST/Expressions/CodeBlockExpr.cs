namespace Parser.AST.Expressions;

public record CodeBlockExpr(Token CodeBlockToken) : IExpression
{
    public int Start => CodeBlockToken.Start;
    public int End => CodeBlockToken.End;
}
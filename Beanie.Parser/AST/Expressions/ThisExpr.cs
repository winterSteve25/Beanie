namespace Parser.AST.Expressions;

public record ThisExpr(Token ThisToken) : IExpression
{
    public int Start => ThisToken.Start;
    public int End => ThisToken.End;
}
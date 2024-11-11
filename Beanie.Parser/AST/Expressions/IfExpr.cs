namespace Parser.AST.Expressions;

public record IfExpr(
    Token IfToken,
    IExpression Condition,
    BlockExpr ThenBlock,
    List<IfExpr.ElseIf> ElseIfClauses,
    IfExpr.Else? ElseClause,
    int Start,
    int End
) : IExpression
{
    public record ElseIf(
        Token ElseToken,
        Token IfToken,
        IExpression Condition,
        BlockExpr ThenBlock,
        int Start,
        int End
    ) : IAstElement;

    public record Else(
        Token ElseToken,
        BlockExpr ElseBlock,
        int Start,
        int End
    ) : IAstElement;
}
namespace Parser.AST.Expressions;

public record IfExpr(
    Token IfToken,
    IExpression Condition,
    BlockExpr ThenBlock,
    List<IfExpr.ElseIfClause> ElseIfClauses,
    IfExpr.ElseClause? Else,
    int Start,
    int End
) : IExpression
{
    public record ElseIfClause(
        Token ElseToken,
        Token IfToken,
        IExpression Condition,
        BlockExpr ThenBlock,
        int Start,
        int End
    ) : IAstElement;

    public record ElseClause(
        Token ElseToken,
        BlockExpr ElseBlock,
        int Start,
        int End
    ) : IAstElement;
}
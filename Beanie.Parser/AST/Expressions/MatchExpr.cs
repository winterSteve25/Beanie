namespace Parser.AST.Expressions;

public record MatchExpr(
    Token MatchToken,
    IExpression Matchee,
    Token CurlyLeft,
    Delimited<MatchExpr.IMatchCase> CaseList,
    Token CurlyRight,
    int Start,
    int End
) : IExpression
{
    public record UnionCase(
        AccessExpr Case,
        Token ParenLeft,
        Delimited<IMatchParam> Params,
        Token ParenRight,
        Token ArrowToken,
        BlockExpr Body,
        int Start,
        int End
    ) : IMatchCase;

    public record AnyCase(
        Token Underscore,
        Token ArrowToken,
        BlockExpr Body,
        int Start,
        int End
    ) : IMatchCase;

    public record ExpressionCase(
        IExpression Expression,
        Token ArrowToken,
        BlockExpr Body,
        int Start,
        int End
    ) : IMatchCase;

    public interface IMatchCase : IAstElement
    {
        Token ArrowToken { get; }
        BlockExpr Body { get; }
    }
}
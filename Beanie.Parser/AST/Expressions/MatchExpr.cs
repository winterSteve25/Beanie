namespace Parser.AST.Expressions;

public record MatchExpr(
    Token MatchToken,
    Token Identifier,
    Token CurlyLeft,
    Delimited<MatchExpr.MatchCase> CaseList,
    Token CurlyRight,
    int Start,
    int End
) : IExpression
{
    public record MatchCase(
        Token CaseToken,
        Token ParenLeft,
        Delimited<IMatchParam> Params,
        Token ParenRight,
        Token ArrowToken,
        BlockExpr Body,
        int Start,
        int End
    ) : IAstElement;
}
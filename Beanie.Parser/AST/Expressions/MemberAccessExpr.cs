namespace Parser.AST.Expressions;

public record MemberAccessExpr(
    IExpression? Left,
    Token? DotToken,
    Token Identifier,
    int Start,
    int End
) : IExpression;
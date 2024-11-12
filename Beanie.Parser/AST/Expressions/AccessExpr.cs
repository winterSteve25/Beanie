namespace Parser.AST.Expressions;

public record AccessExpr(
    IExpression? Left,
    Token? Dot,
    IExpression Right,
    int Start,
    int End
) : IExpression;
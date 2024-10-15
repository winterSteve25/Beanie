namespace Parser.AST.Expressions;

public record BinaryExpr(
    IExpression Left,
    Token Operator,
    IExpression Right,
    int Start,
    int End
) : IExpression;
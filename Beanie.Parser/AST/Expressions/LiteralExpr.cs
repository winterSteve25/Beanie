namespace Parser.AST.Expressions;

public record LiteralExpr(
    Token LiteralToken,
    int Start,
    int End
) : IExpression;
namespace Parser.AST.Expressions;

public record ThisExpr(
    Token ThisToken,
    int Start,
    int End
) : IExpression;
namespace Parser.AST.Expressions;

public record CodeBlockExpr(
    Token CodeBlockToken,
    int Start,
    int End
) : IExpression;
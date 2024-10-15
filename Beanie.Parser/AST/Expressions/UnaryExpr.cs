namespace Parser.AST.Expressions;

public record UnaryExpr(
    Token Operator,
    IExpression Operand,
    int Start,
    int End
) : IExpression;
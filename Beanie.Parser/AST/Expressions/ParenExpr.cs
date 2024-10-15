namespace Parser.AST.Expressions;

public record ParenExpr(
    Token ParenLeft,
    IExpression Inner,
    Token ParenRight,
    int Start,
    int End
) : IExpression;
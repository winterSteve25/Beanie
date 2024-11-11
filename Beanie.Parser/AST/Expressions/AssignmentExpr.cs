namespace Parser.AST.Expressions;

public record AssignmentExpr(
    Identifier Ident,
    Token EqualToken,
    IExpression Right,
    int Start,
    int End
) : IExpression;
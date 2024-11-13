namespace Parser.AST.Expressions;

public record AssignmentExpr(
    MemberAccessExpr Ident,
    Token EqualToken,
    IExpression Right,
    int Start,
    int End
) : IExpression;
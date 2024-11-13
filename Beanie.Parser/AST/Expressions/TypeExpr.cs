namespace Parser.AST.Expressions;

public record TypeExpr(
    MemberAccessExpr Identifier,
    IGeneric? Generic,
    int Start,
    int End
) : IExpression;
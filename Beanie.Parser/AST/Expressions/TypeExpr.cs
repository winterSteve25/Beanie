namespace Parser.AST.Expressions;

public record TypeExpr(
    AccessExpr Identifier,
    IGeneric? Generic,
    int Start,
    int End
) : IExpression;
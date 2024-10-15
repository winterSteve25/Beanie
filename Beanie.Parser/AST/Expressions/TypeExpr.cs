namespace Parser.AST.Expressions;

public record TypeExpr(
    Identifier Identifier,
    IGeneric? Generic,
    int Start,
    int End
) : IExpression;
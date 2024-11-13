namespace Parser.AST.Expressions;

public record FunctionCallExpr(
    MemberAccessExpr Function,
    IGeneric? GenericParam,
    Token ParenLeft,
    Delimited<IExpression>? Arguments,
    Token ParenRight,
    int Start,
    int End
) : IExpression;
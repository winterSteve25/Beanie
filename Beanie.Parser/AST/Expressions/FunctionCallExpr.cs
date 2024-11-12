namespace Parser.AST.Expressions;

public record FunctionCallExpr(
    Identifier Function,
    IGeneric? Generic,
    Token ParenLeft,
    Delimited<IExpression>? Arguments,
    Token ParenRight,
    int Start,
    int End
) : IExpression;
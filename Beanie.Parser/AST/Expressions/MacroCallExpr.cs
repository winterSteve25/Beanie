namespace Parser.AST.Expressions;

public record MacroCallExpr(
    Token AtToken,
    Identifier Macro,
    Token? ParenLeft,
    Delimited<IExpression>? Arguments,
    Token? ParenRight,
    int Start,
    int End
) : IExpression;
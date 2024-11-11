namespace Parser.AST.Generics;

public record CompileTimeGenericArgs(
    Token AngledL,
    Token AngledR,
    Token SquareL,
    Token SquareR,
    Delimited<IExpression>? Args,
    int Start,
    int End
) : IGeneric;
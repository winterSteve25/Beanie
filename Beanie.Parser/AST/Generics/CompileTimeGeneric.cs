namespace Parser.AST;

public record CompileTimeGeneric(
    Token AngledL,
    Token AngledR,
    Token SquareL,
    Token SquareR,
    Delimited<Param> Params,
    int Start,
    int End
) : IGeneric;
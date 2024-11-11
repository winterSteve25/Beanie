namespace Parser.AST.Generics;

public record RuntimeGeneric(
    Token AngledL,
    Token AngeldR,
    Delimited<GenericT>? Generics,
    int Start,
    int End
) : IGeneric;
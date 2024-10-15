namespace Parser.AST;

public record RuntimeGeneric(
    Token AngledL,
    Token AngeldR,
    Delimited<ConstrainedType> Generics,
    int Start,
    int End
) : IGeneric;
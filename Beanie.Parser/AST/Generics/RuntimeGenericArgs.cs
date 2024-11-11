using Parser.AST.Expressions;

namespace Parser.AST.Generics;

public record RuntimeGenericArgs(
    Token AngledL,
    Token AngeldR,
    Delimited<TypeExpr>? Generics,
    int Start,
    int End
) : IGeneric;

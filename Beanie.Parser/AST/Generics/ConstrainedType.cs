using Parser.AST.Expressions;

namespace Parser.AST;

public record ConstrainedType(
    Token Identifier,
    Token? Colon,
    Delimited<TypeExpr> List,
    int Start,
    int End
) : IAstElement;
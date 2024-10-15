using Parser.AST.Expressions;

namespace Parser.AST;

public record Inheritance(
    Token Colon,
    TypeExpr First,
    Delimited<TypeExpr>.Next? Next,
    int Start,
    int End
) : IAstElement;
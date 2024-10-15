using Parser.AST.Expressions;

namespace Parser.AST;

public record Param(
    TypeExpr Type,
    Token Identifier,
    int Start,
    int End
) : IAstElement;
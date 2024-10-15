namespace Parser.AST;

public record Identifier(
    Identifier? Left,
    Token Dot,
    Token Right,
    int Start,
    int End
) : IAstElement;
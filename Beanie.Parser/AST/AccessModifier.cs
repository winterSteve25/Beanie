namespace Parser.AST;

public record AccessModifier(Token Token, int Start, int End) : IAstElement;
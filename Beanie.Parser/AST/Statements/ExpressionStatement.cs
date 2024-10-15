namespace Parser.AST.Statements;

public record ExpressionStatement(
    IExpression Expression,
    Token Semicolon,
    int Start,
    int End
) : IStatement;
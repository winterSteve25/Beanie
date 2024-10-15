namespace Parser.AST.Statements;

public record ReturnStatement(
    Token ReturnToken,
    IExpression Expression,
    Token Semicolon,
    int Start,
    int End
) : IStatement;
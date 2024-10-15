using Parser.AST.Expressions;

namespace Parser.AST.Statements;

public record VariableDeclaration(
    TypeExpr Type,
    Token Identifier,
    Token? EqualsToken,
    IExpression? InitialExpression,
    Token Semicolon,
    int Start,
    int End
) : IStatement;
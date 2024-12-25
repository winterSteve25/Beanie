using Parser.AST.Expressions;

namespace Parser.AST.Statements;

public record AssignmentStatement(
    MemberAccessExpr Member,
    Token EqualToken,
    IExpression Right,
    Token Semicolon,
    int Start,
    int End
) : IStatement;
using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record FieldDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    TypeExpr Type,
    Token Identifier,
    Token? EqualsToken,
    IExpression InitialValue,
    Token Semicolon,
    int Start,
    int End
) : IClassBodyDeclaration;
using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record MethodDeclaration(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    TypeExpr ReturnType,
    Token Identifier,
    Token ParenLeft,
    Delimited<Param> ParamList,
    Token ParenRight,
    Token Semicolon,
    int Start,
    int End
) : IClassBodyDeclaration, IInterfaceMember;
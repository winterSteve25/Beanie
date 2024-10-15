using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record MethodDefinition(
    List<Attribute> Attributes,
    AccessModifier AccessModifier,
    TypeExpr ReturnType,
    Token Identifier,
    Token ParenLeft,
    Delimited<Param> ParamList,
    Token ParenRight,
    Token CurlyLeft,
    FunctionBody Body,
    Token CurlyRight,
    int Start,
    int End
) : IClassBodyDeclaration, IInterfaceMember;
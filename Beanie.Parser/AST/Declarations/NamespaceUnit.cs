using Parser.AST.Expressions;

namespace Parser.AST.Declarations;

public record NamespaceUnit(
    Token? NamespaceToken,
    MemberAccessExpr? Namespace,
    List<IDeclaration> Items,
    int Start,
    int End
) : IDeclaration;
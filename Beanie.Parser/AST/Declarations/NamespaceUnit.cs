using System.Text;

namespace Parser.AST.Declarations;

public record NamespaceUnit(
    Token? NamespaceToken,
    Identifier? Namespace,
    List<IDeclaration> Items,
    int Start,
    int End
) : IDeclaration;
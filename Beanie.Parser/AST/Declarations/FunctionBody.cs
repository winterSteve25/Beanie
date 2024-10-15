namespace Parser.AST.Declarations;

public record FunctionBody(
    List<IStatement> Statements,
    int Start,
    int End
) : IAstElement;
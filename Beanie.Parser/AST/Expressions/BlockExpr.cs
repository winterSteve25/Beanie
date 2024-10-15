namespace Parser.AST.Expressions;

public record BlockExpr(
    Token CurlyLeft,
    List<IStatement> Statements,
    Token CurlyRight,
    int Start,
    int End
) : IExpression;
namespace Parser.AST.Expressions;

public interface IMatchParam : IAstElement
{
}

public record UnderscoreMatchParam(
    Token UnderscoreToken,
    int Start,
    int End
) : IMatchParam;

public record ExpressionMatchParam(
    IExpression Expression,
    int Start,
    int End
) : IMatchParam;
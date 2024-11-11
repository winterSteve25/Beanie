namespace Parser.AST;

public record Delimited<T>(
    T First,
    Delimited<T>.Next? Second,
    Token? Trailing,
    int Start,
    int End
) : IAstElement where T : IAstElement
{
    public record Next(
        Token Delimiter,
        T This,
        Next? Other,
        int Start,
        int End
    ) : IAstElement;
}
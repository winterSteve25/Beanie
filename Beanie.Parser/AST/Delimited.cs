namespace Parser.AST;

public record Delimited<T>(
    T? First,
    Delimited<T>.Next? Second,
    Token? TrailingComma,
    int Start,
    int End
) : IAstElement where T : IAstElement
{
    public static Delimited<T> Empty(int start, int end)
    {
        return new Delimited<T>(default, null, null, start, end);
    }

    public record Next(
        Token Comma,
        T This,
        Next? Other,
        int Start,
        int End
    ) : IAstElement;
}
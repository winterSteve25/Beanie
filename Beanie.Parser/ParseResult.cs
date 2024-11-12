namespace Parser;

public readonly struct ParseResult<T>
{
    public bool Success => _state == ParseResultState.Success;
    public bool IsDifferentConstruct => _state == ParseResultState.WrongConstruct;
    public bool Failed => _state != ParseResultState.Success;

    public readonly T? Value;
    private readonly ParseResultState _state;

    private ParseResult(T? value, ParseResultState state)
    {
        Value = value;
        _state = state;
    }

    public ParseResult<R> Cast<R>()
    {
        if (Value is null)
        {
            return new ParseResult<R>(default, _state);
        }

        return new ParseResult<R>((R)(dynamic)Value, _state);
    }

    public R GetOr<R>(Func<T, R> f, R def)
    {
        return Success ? f(Value!) : def;
    }

    public static ParseResult<T> Successful(T value) =>
        new(value, ParseResultState.Success);

    public static ParseResult<T> Error() =>
        new(default, ParseResultState.Errored);

    public static ParseResult<T> WrongConstruct() =>
        new(default, ParseResultState.WrongConstruct);

    public static ParseResult<T> Inherit<TOther>(ParseResult<TOther> other) =>
        new(default, other._state);
}

public enum ParseResultState
{
    Success,
    Errored,
    WrongConstruct,
}
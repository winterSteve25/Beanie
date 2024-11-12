namespace Parser;

public readonly struct ParseResult<T>
{
    public bool Success => _state == ParseResultState.Success;
    public bool IsDifferentConstruct => _state == ParseResultState.WrongConstruct;
    public bool Failed => _state != ParseResultState.Success;
    public T? Value => _valueLazy.Value;

    private readonly Lazy<T?> _valueLazy;
    private readonly ParseResultState _state;

    private ParseResult(Lazy<T?> value, ParseResultState state)
    {
        _valueLazy = value;
        _state = state;
    }

    public ParseResult<R> Cast<R>()
    {
        if (Value is null)
        {
            R? def = default;
            return new ParseResult<R>(new Lazy<R?>(() => def), _state);
        }

        var res = (R)(dynamic)Value;
        return new ParseResult<R>(new Lazy<R?>(() => res), _state);
    }

    public R GetOr<R>(Func<T, R> f, R def)
    {
        return Success ? f(Value!) : def;
    }

    public static ParseResult<T> Successful(T value) =>
        new(new Lazy<T?>(() => value), ParseResultState.Success);

    public static ParseResult<T> Error() => 
        new(new Lazy<T?>(() => default), ParseResultState.Errored);

    public static ParseResult<T> WrongConstruct() => 
        new(new Lazy<T?>(() => default), ParseResultState.WrongConstruct);

    public static ParseResult<T> Inherit<TOther>(ParseResult<TOther> other) =>
        // new(new Lazy<T?>(() => other.IsDifferentConstruct ? default : producePartial(other.Value!)), other._state);
        new(new Lazy<T?>(() => default), other._state);
}

public enum ParseResultState
{
    Success,
    Errored,
    WrongConstruct,
}
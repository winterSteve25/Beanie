namespace Parser;

public readonly struct Opt<T>
{
    private readonly T? _val;
    private readonly bool _hasValue;

    private Opt(T? val, bool hasVal)
    {
        _val = val;
        _hasValue = hasVal;
    }

    public bool HasValue()
    {
        return _hasValue;
    }

    public bool IsEmpty()
    {
        return !_hasValue;
    }

    public T? Unwrap()
    {
        return _val;
    }

    public R GetOr<R>(Func<T, R> f, R def)
    {
        return _hasValue ? f(_val!) : def;
    }

    public Opt<R> Cast<R>() where R : T
    {
        return new Opt<R>((R) _val, _hasValue);
    }

    public static Opt<T> Some(T val)
    {
        return new Opt<T>(val, true);
    }

    public static Opt<T> None()
    {
        return new Opt<T>(default, false);
    }
}
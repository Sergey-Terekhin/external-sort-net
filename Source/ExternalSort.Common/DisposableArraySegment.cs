namespace ExternalSort;

public readonly struct DisposableArraySegment<T> : IDisposable
{
    private readonly Action<T[]>? _dispose;

    public DisposableArraySegment(ArraySegment<T> value, Action<T[]>? dispose = null)
    {
        _dispose = dispose;
        Value = value;
    }

    public ArraySegment<T> Value { get; }
    public int Count => Value.Count;

    public void Dispose()
    {
        _dispose?.Invoke(Value.Array!);
    }

    public Span<T> AsSpan() => Value.AsSpan();
    public Memory<T> AsMemory() => Value.AsMemory();
}
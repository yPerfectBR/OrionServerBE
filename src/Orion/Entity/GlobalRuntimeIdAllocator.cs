namespace Orion.Entity;

/// <summary>
/// Process-wide entity runtime ID allocation safe for concurrent use (Phase 6b).
/// </summary>
public static class GlobalRuntimeIdAllocator
{
    private static ulong _next;

    public static ulong Allocate()
    {
        return (ulong)Interlocked.Increment(ref _next);
    }

    public static void Seed(ulong minimum)
    {
        ulong current = (ulong)Volatile.Read(ref _next);
        while (minimum > current)
        {
            ulong original = Interlocked.CompareExchange(ref _next, minimum, current);
            if (original == current)
            {
                return;
            }

            current = original;
        }
    }

    internal static void ResetForTests()
    {
        Interlocked.Exchange(ref _next, 0);
    }
}

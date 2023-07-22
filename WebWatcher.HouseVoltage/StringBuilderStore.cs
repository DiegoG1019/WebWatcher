using System.Text;

namespace WebWatcher.HouseVoltage;

public static class StringBuilderStore
{
    [ThreadStatic]
    private static WeakReference<StringBuilder>? Builder;

    public static StringBuilder GetSharedStringBuilder()
    {
        if ((Builder ??= new WeakReference<StringBuilder>(new StringBuilder(1000))).TryGetTarget(out var sb) is false)
            Builder.SetTarget(sb = new StringBuilder(1000));
        return sb.Clear();
    }
}

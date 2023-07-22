using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WebWatcher.HouseVoltage;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct VoltageReading(short Peak, short Valley)
{
    public static unsafe VoltageReading ReadBytes(Span<byte> bytes)
    {
        if (bytes.Length < 4)
            throw new ArgumentException("The span's length must be at least 4 bytes", nameof(bytes));

        short peak, valley;
        fixed (byte* ptr = bytes)
        {
            peak = Unsafe.Read<short>(ptr);
            valley = Unsafe.Read<short>(Unsafe.Add<byte>(ptr, sizeof(short)));
        }

        return new VoltageReading(peak, valley);
    }
}

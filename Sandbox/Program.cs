using WebWatcher.HouseVoltage;

namespace Sandbox;

internal class Program
{
    static unsafe void Main(string[] args)
    {
        Console.WriteLine(sizeof(VoltageReading) == sizeof(UInt32));
    }
}

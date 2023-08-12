using WebWatcher.HouseVoltage;

namespace Sandbox;

internal class Program
{
    private static unsafe void Main(string[] args)
    {
        Console.WriteLine(sizeof(VoltageReport));
    }
}

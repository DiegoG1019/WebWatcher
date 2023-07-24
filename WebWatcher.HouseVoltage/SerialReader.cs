using System.Device.Gpio;

namespace WebWatcher.HouseVoltage;

public class SerialReader
{
    public readonly record struct PinConfiguration(int ClockPin, int SignalAgentWritingPin, int SignalMainReadingPin, int DataPin, int DataBlinkPin);

    public enum Status
    {
        PrematureEnd = -5,
        ArrayTooSmall = -4,
        LatestByteNotFilled = -3,
        DataBlinkTimedOut = -2,
        AgentSignalTimedOut = -1,
        Success = 0,
        ArrayNotFilled = 1
    }

    private readonly SemaphoreSlim Semaphore = new(1, 1);
    private static readonly GpioController Gpio = new();

    private readonly GpioPin Clock;
    private readonly GpioPin SignalAgentWriting;
    private readonly GpioPin SignalMainReading;
    private readonly GpioPin Data;
    private readonly GpioPin DataBlink;
    
    public PinConfiguration Configuration => new(
        Clock.PinNumber,
        SignalAgentWriting.PinNumber,
        SignalMainReading.PinNumber,
        Data.PinNumber,
        DataBlink.PinNumber
    );

    public SerialReader(PinConfiguration configuration) : this(
        configuration.ClockPin,
        configuration.SignalAgentWritingPin,
        configuration.SignalMainReadingPin,
        configuration.DataPin,
        configuration.DataBlinkPin
    )
    { }

    public SerialReader(int clockPin, int signalAgentWritingPin, int signalMainReadingPin, int dataPin, int dataBlinkPin)
    {
        Clock = Gpio.OpenPin(clockPin);
        SignalMainReading = Gpio.OpenPin(signalMainReadingPin);
        SignalAgentWriting = Gpio.OpenPin(signalAgentWritingPin);
        Data = Gpio.OpenPin(dataPin);
        DataBlink = Gpio.OpenPin(dataBlinkPin);

        Clock.SetPinMode(PinMode.Output);
        SignalMainReading.SetPinMode(PinMode.Output);
        SignalAgentWriting.SetPinMode(PinMode.Input);
        Data.SetPinMode(PinMode.Input);
        DataBlink.SetPinMode(PinMode.Input);
    }

    public Status ReadNext(Span<byte> buffer, CancellationToken ct = default)
    {
        Semaphore.Wait(ct);
        try
        {
            bool blink = false;
            int bitIndex = 0;

            SignalMainReading.Write(PinValue.High);
            for (int i = 0; i < 60; i++)
            {
                if (SignalAgentWriting.Read() == PinValue.High)
                    goto AgentWriting;
                Thread.Sleep(1);
            }
            return Status.AgentSignalTimedOut;

            AgentWriting:
            ct.ThrowIfCancellationRequested();
            while (SignalAgentWriting.Read() == PinValue.High)
            {
                Clock.Write(PinValue.High);
                for (int i = 0; i < 20; i++)
                {
                    if (SignalAgentWriting.Read() == PinValue.Low)
                        return Status.PrematureEnd;

                    var bl = DataBlink.Read() == PinValue.High;
                    if (bl != blink)
                    {
                        blink = bl;
                        goto DataInbound;
                    }
                    Thread.Sleep(1);
                }
                return SignalAgentWriting.Read() == PinValue.High ? Status.DataBlinkTimedOut : Status.PrematureEnd;

                DataInbound:
                ct.ThrowIfCancellationRequested();
                buffer[bitIndex / 8] |= (byte)(((int)Data.Read()) << (bitIndex % 8));

                bitIndex++;
                Clock.Write(PinValue.Low);

                if (bitIndex >= (8 * buffer.Length))
                    return Status.ArrayTooSmall;
            }

            if (bitIndex != (8 * buffer.Length))
                return bitIndex % 8 == 0 ? Status.ArrayNotFilled : Status.LatestByteNotFilled;
        }
        finally
        {
            try
            {
                SignalMainReading.Write(PinValue.Low);
                Clock.Write(PinValue.Low);
            }
            finally
            {
                Semaphore.Release(); // The semaphore must be released after the pins are all set up, but it always, ALWAYS, must be released
            }
        }

        return Status.Success;
    }

    public Task<Status> ReadNextAsync(byte[] buffer, CancellationToken ct = default)
        => Task.Run(async () =>
        {
            await Semaphore.WaitAsync(ct);
            try
            {
                bool blink = false;
                int bitIndex = 0;

                SignalMainReading.Write(PinValue.High);
                for (int i = 0; i < 60; i++)
                {
                    if (SignalAgentWriting.Read() == PinValue.High)
                        goto AgentWriting;
                    await Task.Delay(1, ct);
                }
                return Status.AgentSignalTimedOut;

            AgentWriting:
                ct.ThrowIfCancellationRequested();
                while (SignalAgentWriting.Read() == PinValue.High)
                {
                    Clock.Write(PinValue.High);
                    for (int i = 0; i < 20; i++)
                    {
                        if (SignalAgentWriting.Read() == PinValue.Low)
                            return Status.PrematureEnd;

                        var bl = DataBlink.Read() == PinValue.High;
                        if (bl != blink)
                        {
                            blink = bl;
                            goto DataInbound;
                        }
                        await Task.Delay(1);
                    }
                    return SignalAgentWriting.Read() == PinValue.High ? Status.DataBlinkTimedOut : Status.PrematureEnd;

                DataInbound:
                    ct.ThrowIfCancellationRequested();
                    buffer[bitIndex / 8] |= (byte)(((int)Data.Read()) << (bitIndex % 8));

                    bitIndex++;
                    Clock.Write(PinValue.Low);

                    if (bitIndex >= (8 * buffer.Length))
                        return Status.ArrayTooSmall;
                }

                if (bitIndex != (8 * buffer.Length))
                    return bitIndex % 8 == 0 ? Status.ArrayNotFilled : Status.LatestByteNotFilled;
            }
            finally
            {
                try
                {
                    SignalMainReading.Write(PinValue.Low);
                    Clock.Write(PinValue.Low);
                }
                finally
                {
                    Semaphore.Release(); // The semaphore must be released after the pins are all setup, but it always, ALWAYS, must be released
                }
            }

            return Status.Success;
        });
}

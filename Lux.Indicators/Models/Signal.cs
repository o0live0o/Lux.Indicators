namespace Lux.Indicators;

public enum SignalDirection : sbyte
{
    NEUTRAL = 0,
    POSITIVE = 1,
    NEGATIVE = -1
}

public class Signal
{
    public string? Source { get; set; }

    public DateTime Date { get; set; }

    public SignalDirection Direction { get; set; }

    public string? Description { get; set; }
}
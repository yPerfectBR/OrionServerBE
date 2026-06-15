namespace Orion.Events;

public abstract class ServerSignal : ISignal
{
    public abstract ServerEvent Event { get; }
}







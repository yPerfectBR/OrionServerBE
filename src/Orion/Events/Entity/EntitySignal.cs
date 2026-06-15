namespace Orion.Events;

public abstract class EntitySignal : ISignal
{
    public abstract ServerEvent Event { get; }
}







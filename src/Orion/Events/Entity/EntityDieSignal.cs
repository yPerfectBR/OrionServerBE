namespace Orion.Events;

using Orion.Entity;
using Orion.Entity.Traits.Types;

public sealed class EntityDieSignal : EntitySignal, ICancellable
{
    public override ServerEvent Event => ServerEvent.EntityDie;
    public Entity Entity { get; }
    public EntityDeathOptions Options;

    public bool Cancelled
    {
        get => Options.Cancel;
        private set => Options = Options with { Cancel = value };
    }

    public EntityDieSignal(Entity entity, EntityDeathOptions options)
    {
        Entity = entity;
        Options = options;
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }

    internal void SetCancelled(bool cancelled) => Cancelled = cancelled;
}

namespace Orion.Events;

using Orion.Protocol.Enums;
using Entity = Entity.Entity;

public sealed class EntityHurtSignal : EntitySignal, ICancellable
{
    public override ServerEvent Event => ServerEvent.EntityHurt;
    public global::Orion.Entity.Entity Entity { get; }
    public float Amount;
    public ActorDamageCause? Cause { get; }
    public global::Orion.Entity.Entity? Damager { get; }
    public bool Cancelled { get; private set; }

    public EntityHurtSignal(Entity entity, float amount, ActorDamageCause? cause, Entity? damager)
    {
        Entity = entity;
        Amount = amount;
        Cause = cause;
        Damager = damager;
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

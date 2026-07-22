namespace Orion.Api.Traits;

public abstract class EntityTraitBase : TraitBase
{
    public virtual void OnSpawn(EntitySpawnOptions details)
    {
    }

    public virtual void OnTeleport(EntityTeleportDetails details)
    {
    }

    public virtual void OnMove(EntityMoveDetails details)
    {
    }

    public virtual void OnDespawn(EntityDespawnDetails details)
    {
    }
}

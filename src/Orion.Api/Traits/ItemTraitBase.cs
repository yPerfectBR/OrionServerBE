namespace Orion.Api.Traits;

public abstract class ItemTraitBase : TraitBase
{
    public virtual void OnUseOnAir(ItemUseOnAirDetails details)
    {
    }

    public virtual void OnUseOnBlock(ItemUseOnBlockDetails details)
    {
    }

    public virtual void OnPlace(ItemPlaceDetails details)
    {
    }

    public virtual void OnUseOnEntity(ItemUseOnEntityDetails details)
    {
    }

    public virtual void OnUseAttack(ItemUseAttackDetails details)
    {
    }

    public virtual void OnBreakBlock(ItemBreakBlockDetails details)
    {
    }
}

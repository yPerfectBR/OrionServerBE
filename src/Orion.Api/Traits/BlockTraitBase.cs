namespace Orion.Api.Traits;

public abstract class BlockTraitBase : TraitBase
{
    /// <summary>Called after a block is placed into the world.</summary>
    public virtual void OnPlace(BlockPlaceDetails details)
    {
    }
}

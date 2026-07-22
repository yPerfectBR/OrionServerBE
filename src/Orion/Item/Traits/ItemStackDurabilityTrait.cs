namespace Orion.Item.Traits;

public sealed class ItemStackDurabilityTrait : ItemTrait
{
    public static new string Identifier => "item_durability";
    public static readonly string[] Components = ["minecraft:durability"];

    public ItemStackDurabilityTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public void ProcessDamage(Entity.Entity _entity)
    {
    }
}

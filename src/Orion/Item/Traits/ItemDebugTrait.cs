namespace Orion.Item.Traits;

using Orion.Api.Traits;

public sealed class ItemDebugTrait : ItemTrait
{
    public new static string Identifier => "item_debug";

    public ItemDebugTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public override void OnUseOnAir(ItemUseOnAirDetails details) =>
        details.Player.SendMessage("ItemDebugTrait: OnUseOnAir");

    public override void OnUseOnBlock(ItemUseOnBlockDetails details) =>
        details.Player.SendMessage("ItemDebugTrait: OnUseOnBlock");

    public override void OnPlace(ItemPlaceDetails details) =>
        details.Player.SendMessage("ItemDebugTrait: OnPlace");

    public override void OnUseOnEntity(ItemUseOnEntityDetails details) =>
        details.Player.SendMessage($"ItemDebugTrait: OnUseOnEntity target={details.Target.TypeIdentifier}");

    public override void OnUseAttack(ItemUseAttackDetails details) =>
        details.Player.SendMessage($"ItemDebugTrait: OnUseAttack target={details.Target.TypeIdentifier}");
}

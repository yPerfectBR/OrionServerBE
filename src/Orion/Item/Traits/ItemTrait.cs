namespace Orion.Item.Traits;

using System.Reflection;
using Orion.Api.Traits;
using Orion.Protocol.Nbt;

public abstract class ItemTrait : ItemTraitBase
{
    public static readonly string[] Types = [];
    public static readonly string[] Tags = [];
    public static readonly Type? Component = null;
    public static readonly Type[] Components = [];

    protected ItemStack ItemStack { get; }

    public override string Identifier
    {
        get
        {
            if (GetType().GetProperty("Identifier", BindingFlags.Public | BindingFlags.Static) is PropertyInfo property &&
                property.PropertyType == typeof(string) &&
                property.GetValue(null) is string identifier &&
                !string.IsNullOrWhiteSpace(identifier))
            {
                return identifier;
            }

            FieldInfo? identifierField = GetType().GetField("Identifier", BindingFlags.Public | BindingFlags.Static);
            if (identifierField?.GetValue(null) is string fieldIdentifier && !string.IsNullOrWhiteSpace(fieldIdentifier))
            {
                return fieldIdentifier;
            }

            return GetType().FullName ?? GetType().Name;
        }
    }

    protected ItemTrait(ItemStack itemStack)
    {
        ItemStack = itemStack;
    }

    public virtual void OnRead(CompoundTag tag)
    {
    }

    public virtual void OnWrite(CompoundTag tag)
    {
    }
}

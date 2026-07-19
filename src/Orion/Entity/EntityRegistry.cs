using System.Reflection;
using System.Runtime.CompilerServices;
using Orion.Entity.Traits;
using Orion.Protocol.Nbt;

namespace Orion.Entity;

public static class EntityRegistry
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    [ModuleInitializer]
    public static void Initialize() => EnsureLoaded();

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (LoadLock)
        {
            if (_loaded)
            {
                return;
            }

            _ = new EntityType("minecraft:player", ["minecraft:player"]);
            _ = new EntityType("minecraft:item", ["minecraft:item"]);
            EntityTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
            _loaded = true;
        }
    }

    public static CompoundTag BuildAvailableActorIdentifiersTag()
    {
        EnsureLoaded();
        CompoundTag root = new();
        ListTag idList = new();
        foreach (EntityType type in EntityType.GetAll())
        {
            CompoundTag entry = new();
            entry.Set("identifier", new StringTag { Value = type.Identifier });
            ListTag components = new();
            for (int i = 0; i < type.Components.Count; i++)
            {
                components.Values.Add(new StringTag { Value = type.Components[i] });
            }

            entry.Set("components", components);
            idList.Values.Add(entry);
        }

        root.Set("idlist", idList);
        return root;
    }
}

using System.Runtime.CompilerServices;
using Orion.Api.Traits;
using Orion.Entity.Traits;
using Orion.Player.Traits;

namespace Orion.Game.Tests;

/// <summary>
/// Regression: SessionWorker must iterate <see cref="EntityTraitBase"/>, not internal
/// <see cref="EntityTrait"/>. Plugin traits (e.g. inventory) only subclass EntityTraitBase;
/// casting GetTraits() to EntityTrait throws InvalidCastException on login.
/// </summary>
public sealed class SessionTickTraitTests
{
    [Fact]
    public void GetTraits_PluginOnlyTrait_CastToInternalEntityTrait_Throws()
    {
        global::Orion.Entity.Entity entity = new("orion:test_session_tick_entity");
        entity.AddTrait(new PluginOnlySessionTrait());

        Assert.Throws<InvalidCastException>(() =>
        {
            foreach (EntityTrait trait in entity.GetTraits())
            {
                _ = trait;
            }
        });
    }

    [Fact]
    public void GetTraits_PluginOnlyTrait_IteratesAsEntityTraitBase_AndTicksSessionHook()
    {
        global::Orion.Entity.Entity entity = new("orion:test_session_tick_entity_ok");
        PluginOnlySessionTrait trait = new();
        entity.AddTrait(trait);

        int seen = 0;
        foreach (EntityTraitBase baseTrait in entity.GetTraits())
        {
            if (baseTrait is ISessionTickableTrait sessionTickable)
            {
                sessionTickable.OnSessionTick();
                seen++;
            }
        }

        Assert.Equal(1, seen);
        Assert.Equal(1, trait.TickCount);
    }

    [Fact]
    public void SessionWorker_Source_IteratesEntityTraitBase_NotInternalEntityTrait()
    {
        string source = File.ReadAllText(SessionWorkerSourcePath());
        Assert.DoesNotContain(
            "foreach (Entity.Traits.EntityTrait trait in player.GetTraits())",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "foreach (Orion.Api.Traits.EntityTraitBase trait in player.GetTraits())",
            source,
            StringComparison.Ordinal);
    }

    static string SessionWorkerSourcePath([CallerFilePath] string testFile = "")
    {
        string? dir = Path.GetDirectoryName(testFile);
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "src", "Orion", "Scheduling", "SessionWorker.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("SessionWorker.cs not found from test path.");
    }

    sealed class PluginOnlySessionTrait : EntityTraitBase, ISessionTickableTrait
    {
        public new static string Identifier => "orion:test_plugin_session_trait";

        public int TickCount { get; private set; }

        public void OnSessionTick() => TickCount++;
    }
}

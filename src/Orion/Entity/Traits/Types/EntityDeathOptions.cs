namespace Orion.Entity.Traits.Types;
using Orion.Protocol.Enums;

public readonly record struct EntityDeathOptions(
    bool Cancel = false,
    global::Orion.Entity.Entity? KillerSource = null,
    ActorDamageCause? DamageCause = null
);







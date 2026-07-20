namespace Orion.Entity.Traits.Types;

using Orion.Protocol.Types;

/// <param name="ForceFullChunkReload">
/// When true, chunk streaming must unload/resend the view (typically dimension change).
/// When false, the chunk trait may keep already-rendered columns if the destination is in view.
/// </param>
public readonly record struct EntityTeleportOptions(Vec3f From, Vec3f To, bool ForceFullChunkReload = false);

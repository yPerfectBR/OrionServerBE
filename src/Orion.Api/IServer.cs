using Orion.Api.Blocks;
using Orion.Api.Events;
using Orion.Api.Math;
using Orion.Api.Network;

namespace Orion.Api;

public interface IServer
{
    IReadOnlyCollection<IPlayer> OnlinePlayers { get; }
    IWorld? DefaultWorld { get; }
    IWorld? GetWorld(string name);
    IReadOnlyCollection<IWorld> Worlds { get; }

    /// <summary>Dispatches a signal to all subscribed plugin handlers (same bus as host Emit).</summary>
    void Emit(ISignal signal);
}

public interface IWorld
{
    string Name { get; }
    IDimension? GetDimension(string name);
    IReadOnlyCollection<IDimension> Dimensions { get; }
}

public interface IDimension
{
    string Name { get; }
    IWorld World { get; }
    ulong CurrentTick { get; }

    /// <summary>Bedrock difficulty ordinal: 0 Peaceful, 1 Easy, 2 Normal, 3 Hard.</summary>
    int Difficulty { get; }

    IBlock? GetBlock(int x, int y, int z, int layer = 0);
    void SetBlock(int x, int y, int z, IBlock block, int layer = 0, bool dirty = true);
    IBlockPermutation GetPermutation(int x, int y, int z, int layer = 0);
    void SetPermutation(int x, int y, int z, IBlockPermutation permutation, int layer = 0, bool dirty = true);
    IEntity SpawnEntity(string typeIdentifier, Vec3f position, EntitySpawnOptions? options = null);
    IReadOnlyCollection<IEntity> GetEntities();
    void Broadcast(IOutboundPacket packet, PacketBroadcastOptions? options = null);
}

using Orion.Protocol.Types;

namespace Orion.Gameplay;

/// <summary>
/// Opt-in block crack / destroy. Implemented by VanillaMining.
/// </summary>
public interface IPlayerBlockBreakHandler
{
    void OnStartDestroy(global::Orion.Player.Player player, BlockPos pos, int face, ulong tick);

    void OnContinueDestroy(global::Orion.Player.Player player, BlockPos pos, int face, ulong tick);

    void OnCrack(global::Orion.Player.Player player, BlockPos pos, int face, ulong tick);

    void OnAbortDestroy(global::Orion.Player.Player player, BlockPos pos, int face);

    void OnPredictDestroy(global::Orion.Player.Player player, BlockPos pos, int face, ulong tick);

    void OnCreativeDestroy(global::Orion.Player.Player player, BlockPos pos, int face);
}

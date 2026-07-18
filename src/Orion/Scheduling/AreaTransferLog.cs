using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Network;
using Orion.Player;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

namespace Orion.Scheduling;

internal static class AreaTransferLog
{
    public static void Info(PlayerSession? session, string message)
    {
        string full = $"{message} | {FormatExecutionContext()}";
        Log.Info(LogCategory.Orion, "[Area:Transfer] {0}", full);
        SendChat(session, $"§7[Area] §f{full}");
    }

    public static void Warn(PlayerSession? session, string message)
    {
        string full = $"{message} | {FormatExecutionContext()}";
        Log.Warn(LogCategory.Orion, "[Area:Transfer] {0}", full);
        SendChat(session, $"§e[Area] §f{full}");
    }

    public static void Error(PlayerSession? session, string message)
    {
        string full = $"{message} | {FormatExecutionContext()}";
        Log.Error(LogCategory.Orion, "[Area:Transfer] {0}", full);
        SendChat(session, $"§c[Area] §f{full}");
    }

    public static string FormatExecutionContext()
    {
        Thread thread = Thread.CurrentThread;
        string threadName = string.IsNullOrWhiteSpace(thread.Name) ? "unnamed" : thread.Name;
        int cpu = GetCurrentProcessorId();
        return $"tid={thread.ManagedThreadId} thread={threadName} cpu={cpu} pool={thread.IsThreadPoolThread}";
    }

    public static string DescribeEntity(object entity) =>
        entity is Orion.Player.Player player
            ? $"player={player.Username}"
            : entity is IAreaEntity areaEntity
                ? $"entity runtime={areaEntity.RuntimeId}"
                : $"entity={entity.GetType().Name}";

    static void SendChat(PlayerSession? session, string message)
    {
        if (session is null)
        {
            return;
        }

        // Bypass session-worker queue so transfer notices arrive immediately (Basalt sends direct).
        string safeMessage = string.IsNullOrEmpty(message) ? " " : message;
        SessionSendCoordinator.SendDirect(session, new TextPacket
        {
            VariantType = TextVariantType.MessageOnly,
            FilteredMessage = null,
            NeedsTranslation = false,
            Xuid = string.Empty,
            PlatformChatId = string.Empty,
            Variant = new TextVariant
            {
                Message = safeMessage,
                Parameters = [],
                Source = string.Empty,
                Type = TextType.System
            }
        });
    }

    static int GetCurrentProcessorId()
    {
        try
        {
            return Thread.GetCurrentProcessorId();
        }
        catch
        {
            return -1;
        }
    }
}

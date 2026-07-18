namespace Orion.PluginContracts.Messaging;

public interface IPluginMessenger
{
    void Subscribe(string channel, Action<PluginMessage> handler);

    void Unsubscribe(string channel, Action<PluginMessage> handler);

    void Publish(string channel, ReadOnlyMemory<byte> payload, IOrionPlugin? sender = null);
}

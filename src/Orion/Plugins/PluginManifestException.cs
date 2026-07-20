namespace Orion.Plugins;

public sealed class PluginManifestException : InvalidOperationException
{
    public PluginManifestException(string errorCode, string message)
        : base($"[{errorCode}] {message}")
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

namespace Orion.Commands;

public sealed class CommandResult
{
    public bool Success { get; set; }

    public List<string> Messages { get; set; } = [];

    public static CommandResult Empty(bool success = true) =>
        new() { Success = success };

    public static CommandResult Message(string message, bool success = true) =>
        new() { Success = success, Messages = [message] };
}

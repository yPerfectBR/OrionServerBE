using Log = Orion.Logger.Logger;
using Orion.Config;

namespace Orion.Commands;

public static class ConsoleInterface
{
    public static void Run(Server server, CancellationToken cancellationToken, Action requestShutdown)
    {
        Thread.CurrentThread.Name = "server-console";

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string? line = Console.ReadLine();
                if (line is null)
                {
                    continue;
                }

                string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                {
                    continue;
                }

                if (tokens[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    requestShutdown();
                    return;
                }

                HandleResult(server.Commands.Execute(server, line));
            }
            catch (Exception exception)
            {
                Log.Error(LogCategory.System, "Console command failed: {0}", exception);
            }
        }
    }

    static void HandleResult(CommandResult result)
    {
        for (int i = 0; i < result.Messages.Count; i++)
        {
            Log.Chat(result.Messages[i]);
        }
    }
}

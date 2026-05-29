using Qubed.Core.Infrastructure;

namespace Qubed.FrontEnd;

public static class EntryPoint
{
    public static void Main(string[] arguments)
    {
        ILogger logger = null;

        if (arguments.Length == 1 && arguments[0].Equals("log", System.StringComparison.InvariantCultureIgnoreCase))
        {
            logger = new ConsoleLogger();
        }

        using var cube = new Display.Qubed(logger);
        
        cube.Run();
    }
}
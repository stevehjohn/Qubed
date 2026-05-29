using System.Diagnostics.CodeAnalysis;
using Qubed.Console.Tools;

namespace Qubed.Console;

[SuppressMessage("Performance", "CA1806:Do not ignore method results")]
public static class EntryPoint
{
    public static void Main(string[] arguments)
    {
        var iterations = 100;
        
        if (arguments.Length > 0)
        {
            int.TryParse(arguments[0], out iterations);
        }

        SolverBenchmark.Run(iterations);
    }
}
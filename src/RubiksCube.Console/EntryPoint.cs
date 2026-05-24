using System.Diagnostics.CodeAnalysis;
using RubiksCube.Console.Tools;

namespace RubiksCube.Console;

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
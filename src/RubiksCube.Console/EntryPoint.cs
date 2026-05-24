using RubiksCube.Console.Tools;

namespace RubiksCube.Console;

public static class EntryPoint
{
    public static void Main()
    {
        SolverBenchmark.Run(100);
    }
}
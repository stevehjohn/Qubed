using System.Diagnostics;
using RubiksCube.Core;
using RubiksCube.Core.Models;
using static System.Console;

namespace RubiksCube.Console.Tools;

public static class SolverBenchmark
{
    public static void Run(int iterations)
    {
        var totalMoves = 0;

        var totalDuration = TimeSpan.Zero;

        var statistics = new List<(int Moves, TimeSpan Duration)>();

        var stopwatch = Stopwatch.StartNew();

        var longestDuration = TimeSpan.Zero;

        var longestMoves = 0;

        Cube longestDurationCube = null;

        Cube longestMovesCube = null;

        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            var cube = new Cube();

            var random = new Random();

            cube.Scramble(random.Next(20, 50));

            var solver = new Solver(cube);

            WriteLine($"\nIteration {iteration}/{iterations:N0}.\n");

            WriteLine(cube.ToString());
            
            WriteLine();

            var result = solver.Solve();

            if (result.Duration > longestDuration)
            {
                longestDuration = result.Duration;

                longestDurationCube = cube.Clone();
            }

            if (result.Moves.Count > longestMoves)
            {
                longestMoves = result.Moves.Count;
                
                longestMovesCube = cube.Clone();
            }

            foreach (var move in result.Moves)
            {
                cube.ApplyMove(move);
            }

            WriteLine(cube.ToString());
            
            WriteLine();

            totalMoves += result.Moves.Count;

            totalDuration += result.Duration;

            statistics.Add((result.Moves.Count, result.Duration));

            WriteLine(@$"Moves: {result.Moves.Count}, duration: {result.Duration:ss\.fff}s. Average moves: {(double) totalMoves / iteration:N2}, average duration {totalDuration / iteration:ss\.fff}.");
        }

        stopwatch.Stop();

        WriteLine("\nSummary\n-------\n");

        foreach (var statistic in statistics)
        {
            WriteLine(@$"Moves: {statistic.Moves}, duration: {statistic.Duration:ss\.fff}s.");
        }

        WriteLine("      ----            -------");

        WriteLine($"       {totalMoves / iterations}            {totalDuration / iterations:ss\\.fff}");

        WriteLine($"\nMoves range: {statistics.Min(s => s.Moves)} - {longestMoves}.");
        
        var ordered = statistics.OrderBy(s => s.Moves).Select(s => s.Moves).ToArray();

        var median = ordered.Length % 2 == 0
            ? (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) / 2.0
            : ordered[ordered.Length / 2];

        var mode = ordered
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;
        
        WriteLine($"\nMedian: {median}, mode: {mode}.");

        WriteLine($"\nDuration range: {statistics.Min(s => s.Duration):ss\\.fff} - {longestDuration:ss\\.fff}.");

        WriteLine($"\nTotal duration: {totalDuration:mm\\:ss\\.fff}.");

        if (longestDurationCube != null)
        {
            WriteLine($"\nLongest duration cube ({longestDuration:ss\\.fff}):\n");

            WriteLine(longestDurationCube.ToString());
        }

        if (longestMovesCube != null)
        {
            WriteLine($"\nLongest moves cube ({longestMoves}):\n");

            WriteLine(longestMovesCube.ToString());
        }
        
        WriteLine();
    }
}
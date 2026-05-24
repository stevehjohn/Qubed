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
        
        for (var iteration = 1; iteration <= iterations; iteration++)
        {
            var cube = new Cube();
            
            cube.Scramble(Random.Shared.Next(20, 50));
            
            var solver = new Solver(cube);

            WriteLine($"\nIteration {iteration}/{iterations}.\n");
            
            WriteLine(cube.ToString());
            
            var result = solver.Solve();

            foreach (var move in result.Moves)
            {
                cube.ApplyMove(move);
            }

            WriteLine(cube.ToString());
            
            totalMoves += result.Moves.Count;

            totalDuration += result.Duration;
            
            statistics.Add((result.Moves.Count, result.Duration));
            
            WriteLine(@$"Moves: {result.Moves.Count}, duration: {result.Duration:ss\.fff}s. Average moves: {(double) totalMoves / iteration:N2}, average duration {totalDuration / iteration:ss\.fff}.\n");
        }
        
        stopwatch.Stop();
        
        WriteLine("\nSummary\n-------\n");

        foreach (var statistic in statistics)
        {
            WriteLine(@$"Moves: {statistic.Moves,3}, duration: {statistic.Duration:ss\.fff}s.");
        }
        
        WriteLine("      ----            -------");
        
        WriteLine($"       {totalMoves / iterations,3}            {totalDuration / iterations:ss\\.fff}");
        
        WriteLine($"\nMoves range: {statistics.Min(s => s.Moves)} - {statistics.Max(s => s.Moves)}.");
        
        WriteLine($"\nDuration range: {statistics.Min(s => s.Duration):ss\\.fff} - {statistics.Max(s => s.Duration):ss\\.fff}.");
        
        WriteLine($"\nTotal duration: {totalDuration:mm\\.ss\\.fff}.\n");
    }
}
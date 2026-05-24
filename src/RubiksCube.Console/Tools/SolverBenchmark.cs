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
        
        for (var i = 1; i <= iterations; i++)
        {
            var cube = new Cube();
            
            cube.Scramble(Random.Shared.Next(20, 50));
            
            var solver = new Solver(cube);

            WriteLine($"\nIteration {i}/{iterations}.");
            
            WriteLine();
            
            WriteLine(cube.ToString());
            
            var result = solver.Solve();
            
            WriteLine(cube.ToString());
            
            totalMoves += result.Moves.Count;

            totalDuration += result.Duration;
            
            WriteLine($"Moves: {result.Moves.Count}, duration: {result.Duration}. Average moves: {(double) totalMoves / i:N2}, average duration {totalDuration / i}.\n");
        }
    }
}
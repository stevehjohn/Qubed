using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve(Cube cube)
    {
        var moves = new List<Move>();
        
        var stopwatch = Stopwatch.StartNew();

        stopwatch.Stop();
        
        return (false, moves, stopwatch.Elapsed);
    }
}
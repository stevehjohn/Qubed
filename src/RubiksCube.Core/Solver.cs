using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    public (bool Solved, List<Move> Moves, TimeSpan Duration) Solve(Cube cube)
    {
        var stopwatch = Stopwatch.StartNew();
        
        stopwatch.Stop();
        
        return (false, null, stopwatch.Elapsed);
    }
}
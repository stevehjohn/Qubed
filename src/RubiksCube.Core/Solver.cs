using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly Cube _cube;

    public Solver(Cube cube)
    {
        _cube = cube.Clone();
    }

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        var moves = new List<Move>();
        
        var stopwatch = Stopwatch.StartNew();

        stopwatch.Stop();
        
        return (false, moves, stopwatch.Elapsed);
    }
}
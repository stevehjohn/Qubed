using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly Cube _cube;
    
    private static readonly Move[] AllMoves = Enum
        .GetValues<Face>()
        .SelectMany(face => new[]
        {
            new Move(face, Direction.Clockwise),
            new Move(face, Direction.AntiClockwise),
            new Move(face, Direction.HalfTurn)
        })
        .ToArray();
    
    public Solver(Cube cube) => _cube = cube.Clone();

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        var path = new List<Move>();

        if (_cube.IsSolved())
        {
            return (true, path, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        stopwatch.Stop();

        return (false, path, stopwatch.Elapsed);
    }
}

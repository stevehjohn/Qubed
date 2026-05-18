using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly Cube _cube;
    
    private static readonly Move[] AllMoves;

    static Solver()
    {
        var faces = Enum.GetValues<Face>();
        
        AllMoves = new Move[faces.Length * 3];

        var index = 0;
        
        foreach (var face in Enum.GetValues<Face>())
        {
            AllMoves[index++] = new Move(face, Direction.Clockwise);
            
            AllMoves[index++] = new Move(face, Direction.AntiClockwise);
            
            AllMoves[index++] = new Move(face, Direction.HalfTurn);
        }
    }

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

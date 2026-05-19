using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MaxDepth = 20;
    
    private readonly Cube _cube;

    private static readonly Move[] AllMoves;

    private readonly List<Move> _moves = [];

    static Solver()
    {
        var faces = Enum.GetValues<Face>();

        AllMoves = new Move[faces.Length * 3];

        var index = 0;

        foreach (var face in faces)
        {
            AllMoves[index++] = new Move(face, Direction.Clockwise);

            AllMoves[index++] = new Move(face, Direction.AntiClockwise);

            AllMoves[index++] = new Move(face, Direction.HalfTurn);
        }
    }

    public Solver(Cube cube) => _cube = cube.Clone();

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        _moves.Clear();

        if (_cube.IsSolved())
        {
            return (true, null, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine(BruteForce(HasDaisy, MaxDepth));
        
        stopwatch.Stop();
        
        return (_cube.IsSolved(), _moves, stopwatch.Elapsed);
    }

    private bool BruteForce(Func<bool> heuristic, int depth)
    {
        if (depth == 0)
        {
            return false;
        }

        if (heuristic())
        {
            return true;
        }

        foreach (var move in AllMoves)
        {
            if (_moves.Count > 0)
            {
                var last = _moves[^1];

                if (move.Face == last.Face)
                {
                    continue;
                }
            }

            _cube.ApplyMove(move);
            
            _moves.Add(move);

            if (BruteForce(heuristic, depth - 1))
            {
                return true;
            }
            
            _cube.UndoMove();

            _moves.RemoveAt(_moves.Count - 1);
        }

        return false;
    }

    private bool HasDaisy()
    {
        return _cube[Face.Down, 1, 0] == Colour.White
               && _cube[Face.Down, 2, 1] == Colour.White
               && _cube[Face.Down, 1, 2] == Colour.White
               && _cube[Face.Down, 0, 1] == Colour.White;
    }
}
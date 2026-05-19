using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private readonly Cube _cube;

    private static readonly Move[] AllMoves;

    private readonly Stack<Move> _moves = [];

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

        var stopwatch = Stopwatch.StartNew();

        BruteForce(HasDaisy);
        
        stopwatch.Stop();

        if (_cube.IsSolved())
        {
            var moves = _moves.ToList();

            moves.Reverse();
            
            return (true, moves, TimeSpan.Zero);
        }

        return (false, moves, stopwatch.Elapsed);
    }

    private bool BruteForce(Func<bool> heuristic)
    {
        if (heuristic())
        {
            return true;
        }

        foreach (var move in AllMoves)
        {
            if (move == _moves[^1])
            {
                continue;
            }

            _cube.ApplyMove(move);
            
            _moves.Add(move);

            if (BruteForce(heuristic))
            {
                return true;
            }
            
            _cube.UndoMove();
            
            _moves.RemoveAt(_moves.Count - 1);
        }
    }

    private bool HasDaisy()
    {
        return _cube[Face.Down, 1, 0] == Colour.White
               && _cube[Face.Down, 2, 1] == Colour.White
               && _cube[Face.Down, 1, 2] == Colour.White
               && _cube[Face.Down, 0, 1] == Colour.White;
    }
}
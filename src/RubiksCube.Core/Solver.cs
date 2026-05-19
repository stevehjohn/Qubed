using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MaxDepth = 12;

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
            Console.WriteLine(_cube.ToString());

            return (true, _moves, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine(_cube.ToString());

        Console.WriteLine(BruteForce(HasDaisy, MaxDepth));

        Console.WriteLine(BruteForce(HasWhiteCross, MaxDepth));

        Console.WriteLine(BruteForce(HasRgwCorner, MaxDepth));

        Console.WriteLine(BruteForce(HasRgwRbwCorners, MaxDepth)); 

        Console.WriteLine(BruteForce(HasRgwRbwBwoCorners, MaxDepth));
        //
        // Console.WriteLine(BruteForce(HasAllCorners, MaxDepth));

        Console.WriteLine(_cube.ToString());

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

                if (AxisOf(move.Face) == AxisOf(last.Face))
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

    private static int AxisOf(Face face)
    {
        return face switch
        {
            Face.Left or Face.Right => 0,
            Face.Up or Face.Down => 1,
            Face.Front or Face.Back => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }

    private bool HasDaisy()
    {
        return _cube[Face.Down, 1, 0] == Colour.White
               && _cube[Face.Down, 2, 1] == Colour.White
               && _cube[Face.Down, 1, 2] == Colour.White
               && _cube[Face.Down, 0, 1] == Colour.White;
    }

    private bool HasWhiteCross()
    {
        return _cube[Face.Up, 1, 0] == Colour.White
               && _cube[Face.Up, 2, 1] == Colour.White
               && _cube[Face.Up, 1, 2] == Colour.White
               && _cube[Face.Up, 0, 1] == Colour.White
               && _cube[Face.Left, 1, 0] == Colour.Green
               && _cube[Face.Front, 1, 0] == Colour.Red
               && _cube[Face.Right, 1, 0] == Colour.Blue
               && _cube[Face.Back, 1, 0] == Colour.Orange;
    }

    private bool HasRgwCorner()
    {
        return HasWhiteCross()
               && _cube[Face.Up, 0, 2] == Colour.White
               && _cube[Face.Left, 2, 0] == Colour.Green
               && _cube[Face.Front, 0, 0] == Colour.Red;
    }

    private bool HasRgwRbwCorners()
    {
        return HasWhiteCross()
               && HasRgwCorner()
               && _cube[Face.Up, 2, 2] == Colour.White
               && _cube[Face.Front, 2, 0] == Colour.Red
               && _cube[Face.Right, 0, 0] == Colour.Blue;
    }

    private bool HasRgwRbwBwoCorners()
    {
        return HasWhiteCross()
               && HasRgwCorner()
               && _cube[Face.Up, 2, 2] == Colour.White
               && _cube[Face.Right, 2, 0] == Colour.Blue
               && _cube[Face.Back, 0, 0] == Colour.Orange;
    }

    private bool HasAllCorners()
    {
        return HasRgwRbwBwoCorners()
               && _cube[Face.Up, 2, 0] == Colour.White;
    }
}
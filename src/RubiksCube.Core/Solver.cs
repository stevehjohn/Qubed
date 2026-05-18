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

        foreach (var face in faces)
        {
            AllMoves[index++] = new Move(face, Direction.Clockwise);

            AllMoves[index++] = new Move(face, Direction.AntiClockwise);

            AllMoves[index++] = new Move(face, Direction.HalfTurn);
        }
    }

    public Solver(Cube cube) => _cube = cube.Clone();

    private readonly List<Move> _moves = [];

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve()
    {
        _moves.Clear();

        if (_cube.IsSolved())
        {
            return (true, _moves, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        FindWhiteCross();
        
        stopwatch.Stop();

        return (false, _moves, stopwatch.Elapsed);
    }

    private void FindWhiteCross()
    {
        Console.WriteLine(SearchWhiteCross(40, null));
    }

    private bool SearchWhiteCross(int depth, Face? previousFace)
    {
        if (HasWhiteCross())
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        foreach (var move in AllMoves)
        {
            if (move.Face == previousFace || IsSameAxis(move.Face, previousFace))
            {
                continue;
            }

            _cube.ApplyMove(move.Face, move.Direction);
            
            _moves.Add(move);

            if (SearchWhiteCross(depth - 1, move.Face))
            {
                return true;
            }

            _moves.RemoveAt(_moves.Count - 1);
            
            _cube.UndoMove();
        }

        return false;
    }

    private static bool IsSameAxis(Face face, Face? previousFace)
    {
        if (previousFace is null)
        {
            return false;
        }

        return (face, previousFace.Value) switch
        {
            (Face.Up, Face.Down) or (Face.Down, Face.Up) => true,
            (Face.Left, Face.Right) or (Face.Right, Face.Left) => true,
            (Face.Front, Face.Back) or (Face.Back, Face.Front) => true,
            _ => false
        };
    }

    private bool HasWhiteCross()
    {
        return
            _cube[Face.Up, 1, 0] == Colour.White &&
            _cube[Face.Up, 0, 1] == Colour.White &&
            _cube[Face.Up, 2, 1] == Colour.White &&
            _cube[Face.Up, 1, 2] == Colour.White;
    }
}

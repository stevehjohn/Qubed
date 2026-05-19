using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MinDepth = 1;

    private const int MaxDepth = 15;

    private readonly Cube _cube;

    private static readonly Move[] AllMoves;

    private readonly List<Move> _moves = [];

    private readonly HashSet<(ulong A, ulong B, ulong C, int D)> _visited = [];

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

        Console.WriteLine(BruteForce(HasDaisy));

        Console.WriteLine(BruteForce(HasWhiteCross));
        
        Console.WriteLine("\nCorners\n");

        Console.WriteLine(BruteForce(HasRgwCorner));

        Console.WriteLine(BruteForce(HasRbwCorners));

        Console.WriteLine(BruteForce(HasRgwWboCorners));

        Console.WriteLine(BruteForce(HasGwoCorners));
        
        Console.WriteLine("\nMiddle\n");

        Console.WriteLine(BruteForce(HasRedGreenMiddle));

        Console.WriteLine(BruteForce(HasRedBlueMiddle));

        Console.WriteLine(BruteForce(HasOrangeGreenMiddle));

        Console.WriteLine(BruteForce(HasBlueOrangeMiddle));

        Console.WriteLine("\nYellow Cross\n");

        Console.WriteLine(BruteForce(HasYellowCross));
        
        Console.WriteLine("\nYellow Edges\n");

        Console.WriteLine(BruteForce(HasYellowRedEdge));
        
        Console.WriteLine(BruteForce(HasYellowBlueEdge));

        Console.WriteLine(BruteForce(HasYellowOrangeEdge));

        Console.WriteLine(BruteForce(HasYellowGreenEdge));
        
        Console.WriteLine("\nRemaining Corners\n");
        
        Console.WriteLine(BruteForce(HasGryCorner));

        Console.WriteLine(BruteForce(HasRbyCorner));

        Console.WriteLine(BruteForce(HasGoyCorner));

        Console.WriteLine(_cube.ToString());

        stopwatch.Stop();

        return (_cube.IsSolved(), _moves, stopwatch.Elapsed);
    }

    private bool BruteForce(Func<bool> heuristic, int minDepth = MinDepth)
    {
        var stopwatch = new Stopwatch();
        
        for (var depth = minDepth; depth <= MaxDepth; depth++)
        {
            Console.Write(depth);

            stopwatch.Restart();
            
            _visited.Clear();

            var result = Search(heuristic, depth);
            
            Console.WriteLine($" {stopwatch.Elapsed}");

            if (result)
            {
                return true;
            }
        }

        return false;
    }

    private bool Search(Func<bool> heuristic, int depth)
    {
        if (heuristic())
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        var hash = _cube.GetHash();

        if (! _visited.Add((hash.A, hash.B, hash.C, depth)))
        {
            return false;
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
                    if (move.Face < last.Face)
                    {
                        continue;
                    }
                }
            }

            _cube.ApplyMove(move);

            _moves.Add(move);

            if (Search(heuristic, depth - 1))
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

    private bool HasRbwCorners()
    {
        return HasRgwCorner()
               && _cube[Face.Up, 2, 2] == Colour.White
               && _cube[Face.Front, 2, 0] == Colour.Red
               && _cube[Face.Right, 0, 0] == Colour.Blue;
    }

    private bool HasRgwWboCorners()
    {
        return HasRbwCorners()
               && _cube[Face.Up, 2, 0] == Colour.White
               && _cube[Face.Right, 2, 0] == Colour.Blue
               && _cube[Face.Back, 0, 0] == Colour.Orange;
    }

    private bool HasGwoCorners()
    {
        return HasRgwWboCorners()
               && _cube[Face.Up, 0, 0] == Colour.White
               && _cube[Face.Left, 0, 0] == Colour.Green
               && _cube[Face.Back, 2, 0] == Colour.Orange;
    }

    private bool HasRedGreenMiddle()
    {
        return HasGwoCorners()
               && _cube[Face.Front, 0, 1] == Colour.Red
               && _cube[Face.Left, 2, 1] == Colour.Green;
    }

    private bool HasRedBlueMiddle()
    {
        return HasRedGreenMiddle()
               && _cube[Face.Front, 2, 1] == Colour.Red
               && _cube[Face.Right, 0, 1] == Colour.Blue;
    }

    private bool HasOrangeGreenMiddle()
    {
        return HasRedBlueMiddle()
               && _cube[Face.Back, 2, 1] == Colour.Orange
               && _cube[Face.Left, 0, 1] == Colour.Green;
    }

    private bool HasBlueOrangeMiddle()
    {
        return HasOrangeGreenMiddle()
               && _cube[Face.Right, 2, 1] == Colour.Blue
               && _cube[Face.Back, 0, 1] == Colour.Orange;
    }

    private bool HasYellowCross()
    {
        return HasBlueOrangeMiddle()
               && _cube[Face.Down, 1, 0] == Colour.Yellow
               && _cube[Face.Down, 2, 1] == Colour.Yellow
               && _cube[Face.Down, 1, 2] == Colour.Yellow
               && _cube[Face.Down, 0, 1] == Colour.Yellow;
    }

    private bool HasYellowRedEdge()
    {
        return HasYellowCross()
               && _cube[Face.Front, 1, 2] == Colour.Red;
    }

    private bool HasYellowBlueEdge()
    {
        return HasYellowRedEdge()
               && _cube[Face.Right, 1, 2] == Colour.Blue;
    }

    private bool HasYellowOrangeEdge()
    {
        return HasYellowBlueEdge()
               && _cube[Face.Back, 1, 2] == Colour.Orange;
    }

    private bool HasYellowGreenEdge()
    {
        return HasYellowOrangeEdge()
               && _cube[Face.Left, 1, 2] == Colour.Green;
    }

    private bool HasGryCorner()
    {
        return HasYellowGreenEdge()
               && _cube[Face.Left, 2, 2] == Colour.Green
               && _cube[Face.Front, 0, 2] == Colour.Red
               && _cube[Face.Down, 0, 0] == Colour.Yellow;
    }

    private bool HasRbyCorner()
    {
        return HasGryCorner()
               && _cube[Face.Front, 2, 2] == Colour.Red
               && _cube[Face.Right, 2, 0] == Colour.Blue
               && _cube[Face.Down, 2, 0] == Colour.Yellow;
    }

    private bool HasGoyCorner()
    {
        return HasRbyCorner()
               && _cube[Face.Left, 2, 0] == Colour.Green
               && _cube[Face.Back, 2, 2] == Colour.Orange
               && _cube[Face.Down, 0, 2] == Colour.Yellow;
    }

    private bool HasBoyCorner()
    {
        return HasGryCorner()
               && _cube[Face.Back, 2, 0] == Colour.Orange
               && _cube[Face.Right, 2, 2] == Colour.Blue
               && _cube[Face.Down, 2, 2] == Colour.Yellow;
    }
}
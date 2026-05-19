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

        Console.WriteLine(BruteForce(HasBoyCorner));

        Console.WriteLine(_cube.ToString());

        stopwatch.Stop();

        Console.WriteLine();

        Console.WriteLine(stopwatch.Elapsed);

        return (_cube.IsSolved(), _moves, stopwatch.Elapsed);
    }

    private bool BruteForce(Func<Cube, bool> heuristic, int minDepth = MinDepth)
    {
        var stopwatch = new Stopwatch();
        
        for (var depth = minDepth; depth <= MaxDepth; depth++)
        {
            Console.Write(depth);

            stopwatch.Restart();
            
            var found = false;
            
            List<Move> foundMoves = null;

            var innerDepth = depth;
            
            Parallel.ForEach(AllMoves, new ParallelOptions(), (move, state) =>
            {
                var cubeCopy = _cube.Clone();
                
                var newMoves = new List<Move> { move };
                
                var visited = new HashSet<(ulong, ulong, ulong, int)>();

                cubeCopy.ApplyMove(move);

                if (Search(heuristic, cubeCopy, newMoves, move, visited, innerDepth - 1))
                {
                    lock (state)
                    {
                        found = true;
                        
                        foundMoves = newMoves;
                    }

                    state.Stop();
                }
            });
            
            Console.WriteLine($" {stopwatch.Elapsed}");
            
            if (found)
            {
                _moves.AddRange(foundMoves);
                
                return true;
            }
        }

        return false;
    }

    private bool Search(Func<Cube, bool> heuristic, Cube cube, List<Move> moves, Move lastMove, HashSet<(ulong, ulong, ulong, int)> visited, int depth)
    {
        if (heuristic(cube))
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        var hash = cube.GetHash();

        var key = (hash.A, hash.B, hash.C, depth);

        if (! visited.Add(key))
        {
            return false;
        }

        foreach (var move in AllMoves)
        {
            if (moves.Count > 0)
            {
                if (move.Face == lastMove.Face)
                {
                    continue;
                }

                if (AxisOf(move.Face) == AxisOf(lastMove.Face))
                {
                    if (move.Face < lastMove.Face)
                    {
                        continue;
                    }
                }
            }

            cube.ApplyMove(move);

            moves.Add(move);

            if (Search(heuristic, cube, moves, move, visited, depth - 1))
            {
                return true;
            }

            cube.UndoMove();

            moves.RemoveAt(moves.Count - 1);
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

    private bool HasDaisy(Cube cube)
    {
        return cube[Face.Down, 1, 0] == Colour.White
               && cube[Face.Down, 2, 1] == Colour.White
               && cube[Face.Down, 1, 2] == Colour.White
               && cube[Face.Down, 0, 1] == Colour.White;
    }

    private bool HasWhiteCross(Cube cube)
    {
        return cube[Face.Up, 1, 0] == Colour.White
               && cube[Face.Up, 2, 1] == Colour.White
               && cube[Face.Up, 1, 2] == Colour.White
               && cube[Face.Up, 0, 1] == Colour.White
               && cube[Face.Left, 1, 0] == Colour.Green
               && cube[Face.Front, 1, 0] == Colour.Red
               && cube[Face.Right, 1, 0] == Colour.Blue
               && cube[Face.Back, 1, 0] == Colour.Orange;
    }

    private bool HasRgwCorner(Cube cube)
    {
        return HasWhiteCross(cube)
               && cube[Face.Up, 0, 2] == Colour.White
               && cube[Face.Left, 2, 0] == Colour.Green
               && cube[Face.Front, 0, 0] == Colour.Red;
    }

    private bool HasRbwCorners(Cube cube)
    {
        return HasRgwCorner(cube)
               && cube[Face.Up, 2, 2] == Colour.White
               && cube[Face.Front, 2, 0] == Colour.Red
               && cube[Face.Right, 0, 0] == Colour.Blue;
    }

    private bool HasRgwWboCorners(Cube cube)
    {
        return HasRbwCorners(cube)
               && cube[Face.Up, 2, 0] == Colour.White
               && cube[Face.Right, 2, 0] == Colour.Blue
               && cube[Face.Back, 0, 0] == Colour.Orange;
    }

    private bool HasGwoCorners(Cube cube)
    {
        return HasRgwWboCorners(cube)
               && cube[Face.Up, 0, 0] == Colour.White
               && cube[Face.Left, 0, 0] == Colour.Green
               && cube[Face.Back, 2, 0] == Colour.Orange;
    }

    private bool HasRedGreenMiddle(Cube cube)
    {
        return HasGwoCorners(cube)
               && cube[Face.Front, 0, 1] == Colour.Red
               && cube[Face.Left, 2, 1] == Colour.Green;
    }

    private bool HasRedBlueMiddle(Cube cube)
    {
        return HasRedGreenMiddle(cube)
               && cube[Face.Front, 2, 1] == Colour.Red
               && cube[Face.Right, 0, 1] == Colour.Blue;
    }

    private bool HasOrangeGreenMiddle(Cube cube)
    {
        return HasRedBlueMiddle(cube)
               && cube[Face.Back, 2, 1] == Colour.Orange
               && cube[Face.Left, 0, 1] == Colour.Green;
    }

    private bool HasBlueOrangeMiddle(Cube cube)
    {
        return HasOrangeGreenMiddle(cube)
               && cube[Face.Right, 2, 1] == Colour.Blue
               && cube[Face.Back, 0, 1] == Colour.Orange;
    }

    private bool HasYellowCross(Cube cube)
    {
        return HasBlueOrangeMiddle(cube)
               && cube[Face.Down, 1, 0] == Colour.Yellow
               && cube[Face.Down, 2, 1] == Colour.Yellow
               && cube[Face.Down, 1, 2] == Colour.Yellow
               && cube[Face.Down, 0, 1] == Colour.Yellow;
    }

    private bool HasYellowRedEdge(Cube cube)
    {
        return HasYellowCross(cube)
               && cube[Face.Front, 1, 2] == Colour.Red;
    }

    private bool HasYellowBlueEdge(Cube cube)
    {
        return HasYellowRedEdge(cube)
               && cube[Face.Right, 1, 2] == Colour.Blue;
    }

    private bool HasYellowOrangeEdge(Cube cube)
    {
        return HasYellowBlueEdge(cube)
               && cube[Face.Back, 1, 2] == Colour.Orange;
    }

    private bool HasYellowGreenEdge(Cube cube)
    {
        return HasYellowOrangeEdge(cube)
               && cube[Face.Left, 1, 2] == Colour.Green;
    }

    private bool HasGryCorner(Cube cube)
    {
        return HasYellowGreenEdge(cube)
               && cube[Face.Left, 2, 2] == Colour.Green
               && cube[Face.Front, 0, 2] == Colour.Red
               && cube[Face.Down, 0, 0] == Colour.Yellow;
    }

    private bool HasRbyCorner(Cube cube)
    {
        return HasGryCorner(cube)
               && cube[Face.Front, 2, 2] == Colour.Red
               && cube[Face.Right, 0, 2] == Colour.Blue
               && cube[Face.Down, 2, 0] == Colour.Yellow;
    }

    private bool HasGoyCorner(Cube cube)
    {
        return HasRbyCorner(cube)
               && cube[Face.Left, 0, 2] == Colour.Green
               && cube[Face.Back, 2, 2] == Colour.Orange
               && cube[Face.Down, 0, 2] == Colour.Yellow;
    }

    private bool HasBoyCorner(Cube cube)
    {
        return HasGoyCorner(cube)
               && cube[Face.Back, 0, 2] == Colour.Orange
               && cube[Face.Right, 2, 2] == Colour.Blue
               && cube[Face.Down, 2, 2] == Colour.Yellow;
    }
}
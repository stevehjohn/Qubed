using System.Diagnostics;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MinDepth = 6;

    private const int MaxDepth = 15;

    private readonly Cube _cube;

    private static readonly Move[] AllMoves =
    [
        new(Face.Down, Direction.Clockwise),
        new(Face.Down, Direction.AntiClockwise),
        new(Face.Down, Direction.HalfTurn),

        new(Face.Front, Direction.Clockwise),
        new(Face.Front, Direction.AntiClockwise),
        new(Face.Front, Direction.HalfTurn),

        new(Face.Right, Direction.Clockwise),
        new(Face.Right, Direction.AntiClockwise),
        new(Face.Right, Direction.HalfTurn),

        new(Face.Left, Direction.Clockwise),
        new(Face.Left, Direction.AntiClockwise),
        new(Face.Left, Direction.HalfTurn),

        new(Face.Back, Direction.Clockwise),
        new(Face.Back, Direction.AntiClockwise),
        new(Face.Back, Direction.HalfTurn),

        new(Face.Up, Direction.Clockwise),
        new(Face.Up, Direction.AntiClockwise),
        new(Face.Up, Direction.HalfTurn)
    ];

    private readonly List<Move> _moves = [];

    public Solver(Cube cube) => _cube = cube.Clone();

    public void SolveAsync(Action<(bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration)> callback, Action<List<Move>> stepCallback = null)
    {
        Task.Run(() => Solve(stepCallback))
            .ContinueWith(task => { callback(task.Result); }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    private (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve(Action<List<Move>> stepCallback)
    {
        _moves.Clear();

        if (_cube.IsSolved())
        {
            Console.WriteLine(_cube.ToString());

            return (true, _moves, TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine(_cube.ToString());

        Console.WriteLine(BruteForce(HasDaisy, stepCallback));

        Console.WriteLine(BruteForce(HasWhiteCross, stepCallback));

        Console.WriteLine("\nCorners\n");

        Console.WriteLine(BruteForce(HasRgwCorner, stepCallback));

        Console.WriteLine(BruteForce(HasRbwCorners, stepCallback));

        Console.WriteLine(BruteForce(HasRgwWboCorners, stepCallback));

        Console.WriteLine(BruteForce(HasGwoCorners, stepCallback));

        Console.WriteLine("\nMiddle\n");

        Console.WriteLine(BruteForce(HasRedGreenMiddle, stepCallback));

        Console.WriteLine(BruteForce(HasRedBlueMiddle, stepCallback));

        Console.WriteLine(BruteForce(HasOrangeGreenMiddle, stepCallback));

        Console.WriteLine(BruteForce(HasBlueOrangeMiddle, stepCallback));

        Console.WriteLine("\nYellow Cross\n");

        Console.WriteLine(BruteForce(HasYellowCross, stepCallback, true));

        Console.WriteLine("\nYellow Edges\n");

        Console.WriteLine(BruteForce(HasAlignedYellowCross, stepCallback, true));

        Console.WriteLine("\nRemaining Corners\n");

        Console.WriteLine(BruteForce(HasGryCorner, stepCallback));
        
        Console.WriteLine(BruteForce(HasRbyCorner, stepCallback));
        
        Console.WriteLine(BruteForce(HasGoyCorner, stepCallback));
        
        Console.WriteLine(BruteForce(HasBoyCorner, stepCallback));

        Console.WriteLine(_cube.ToString());

        stopwatch.Stop();

        Console.WriteLine();

        Console.WriteLine(stopwatch.Elapsed);

        return (true, _moves, stopwatch.Elapsed);
    }

    private bool BruteForce(Func<Cube, bool> heuristic, Action<List<Move>> stepCallback, bool excludeUpFace = false)
    {
        var stopwatch = new Stopwatch();

        for (var depth = MinDepth; depth <= MaxDepth; depth++)
        {
            Console.Write(depth);

            stopwatch.Restart();

            var found = false;

            List<Move> foundMoves = null;

            var innerDepth = depth;

            Parallel.ForEach(AllMoves, new ParallelOptions(), (move, state) =>
            {
                if (move.Face == Face.Up && excludeUpFace)
                {
                    return;
                }

                var cubeCopy = _cube.Clone();

                var newMoves = new List<Move> { move };

                cubeCopy.ApplyMove(move);

                if (Search(heuristic, cubeCopy, newMoves, move, innerDepth - 1, excludeUpFace))
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

                foreach (var move in foundMoves)
                {
                    _cube.ApplyMove(move);
                }
                
                stepCallback(foundMoves);

                return true;
            }
        }

        return false;
    }

    private bool Search(Func<Cube, bool> heuristic, Cube cube, List<Move> moves, Move lastMove, int depth, bool excludeUpFace)
    {
        if (heuristic(cube))
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        foreach (var move in AllMoves)
        {
            if (move.Face == Face.Up && excludeUpFace)
            {
                continue;
            }

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

            if (Search(heuristic, cube, moves, move, depth - 1, excludeUpFace))
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

    private static bool HasDaisy(Cube cube)
    {
        return cube[Face.Down, 1, 0] == Colour.White
               && cube[Face.Down, 2, 1] == Colour.White
               && cube[Face.Down, 1, 2] == Colour.White
               && cube[Face.Down, 0, 1] == Colour.White;
    }

    private static bool HasWhiteCross(Cube cube)
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

    private static bool HasRgwCorner(Cube cube)
    {
        return HasWhiteCross(cube)
               && cube[Face.Up, 0, 2] == Colour.White
               && cube[Face.Left, 2, 0] == Colour.Green
               && cube[Face.Front, 0, 0] == Colour.Red;
    }

    private static bool HasRbwCorners(Cube cube)
    {
        return HasRgwCorner(cube)
               && cube[Face.Up, 2, 2] == Colour.White
               && cube[Face.Front, 2, 0] == Colour.Red
               && cube[Face.Right, 0, 0] == Colour.Blue;
    }

    private static bool HasRgwWboCorners(Cube cube)
    {
        return HasRbwCorners(cube)
               && cube[Face.Up, 2, 0] == Colour.White
               && cube[Face.Right, 2, 0] == Colour.Blue
               && cube[Face.Back, 0, 0] == Colour.Orange;
    }

    private static bool HasGwoCorners(Cube cube)
    {
        return HasRgwWboCorners(cube)
               && cube[Face.Up, 0, 0] == Colour.White
               && cube[Face.Left, 0, 0] == Colour.Green
               && cube[Face.Back, 2, 0] == Colour.Orange;
    }

    private static bool HasRedGreenMiddle(Cube cube)
    {
        return HasGwoCorners(cube)
               && cube[Face.Front, 0, 1] == Colour.Red
               && cube[Face.Left, 2, 1] == Colour.Green;
    }

    private static bool HasRedBlueMiddle(Cube cube)
    {
        return HasRedGreenMiddle(cube)
               && cube[Face.Front, 2, 1] == Colour.Red
               && cube[Face.Right, 0, 1] == Colour.Blue;
    }

    private static bool HasOrangeGreenMiddle(Cube cube)
    {
        return HasRedBlueMiddle(cube)
               && cube[Face.Back, 2, 1] == Colour.Orange
               && cube[Face.Left, 0, 1] == Colour.Green;
    }

    private static bool HasBlueOrangeMiddle(Cube cube)
    {
        return HasOrangeGreenMiddle(cube)
               && cube[Face.Right, 2, 1] == Colour.Blue
               && cube[Face.Back, 0, 1] == Colour.Orange;
    }

    private static bool HasYellowCross(Cube cube)
    {
        return HasBlueOrangeMiddle(cube)
               && cube[Face.Down, 1, 0] == Colour.Yellow
               && cube[Face.Down, 2, 1] == Colour.Yellow
               && cube[Face.Down, 1, 2] == Colour.Yellow
               && cube[Face.Down, 0, 1] == Colour.Yellow;
    }

    private static bool HasAlignedYellowCross(Cube cube)
    {
        return HasYellowCross(cube)
               && cube[Face.Front, 1, 2] == Colour.Red
               && cube[Face.Right, 1, 2] == Colour.Blue
               && cube[Face.Back, 1, 2] == Colour.Orange
               && cube[Face.Left, 1, 2] == Colour.Green;
    }

    private static bool HasGryCorner(Cube cube)
    {
        return HasAlignedYellowCross(cube)
               && cube[Face.Left, 2, 2] == Colour.Green
               && cube[Face.Front, 0, 2] == Colour.Red
               && cube[Face.Down, 0, 0] == Colour.Yellow;
    }

    private static bool HasRbyCorner(Cube cube)
    {
        return HasGryCorner(cube)
               && cube[Face.Front, 2, 2] == Colour.Red
               && cube[Face.Right, 0, 2] == Colour.Blue
               && cube[Face.Down, 2, 0] == Colour.Yellow;
    }

    private static bool HasGoyCorner(Cube cube)
    {
        return HasRbyCorner(cube)
               && cube[Face.Left, 0, 2] == Colour.Green
               && cube[Face.Back, 2, 2] == Colour.Orange
               && cube[Face.Down, 0, 2] == Colour.Yellow;
    }

    private static bool HasBoyCorner(Cube cube)
    {
        return HasGoyCorner(cube)
               && cube[Face.Back, 0, 2] == Colour.Orange
               && cube[Face.Right, 2, 2] == Colour.Blue
               && cube[Face.Down, 2, 2] == Colour.Yellow;
    }
}
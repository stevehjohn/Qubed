using System.Diagnostics;
using RubiksCube.Core.Logic;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MinDepth = 1;

    private const int MaxDepth = 15;

    private readonly Cube _cube;

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

        var checks = new List<Func<Cube, bool>>();

        var solved = true;

        foreach (var algorithm in AlgorithmLibrary.Algorithms)
        {
            Console.WriteLine($"{algorithm.Name}\n");
            
            checks.AddRange(algorithm.IsCompleteChecks);

            solved &= BruteForceAlgorithm(checks, algorithm.MoveSets, stepCallback);
            
            Console.WriteLine();
        }

        Console.WriteLine(_cube.ToString());

        foreach (var move in _moves)
        {
            Console.WriteLine(move.ToString());
        }

        stopwatch.Stop();

        Console.WriteLine();

        Console.WriteLine($"Moves: {_moves.Count}. Duration: {stopwatch.Elapsed}");

        return (solved, _moves, stopwatch.Elapsed);
    }

    private bool BruteForceAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSets, Action<List<Move>> stepCallback)
    {
        var stopwatch = new Stopwatch();

        for (var depth = MinDepth; depth <= MaxDepth; depth++)
        {
            Console.Write(depth);

            stopwatch.Restart();

            var found = false;

            List<Move> foundMoves = null;

            var innerDepth = depth;

            Parallel.ForEach(moveSets, new ParallelOptions(), (moveSet, state) =>
            {
                var cubeCopy = _cube.Clone();

                var newMoves = new List<Move>(moveSet);

                foreach (var move in moveSet)
                {
                    cubeCopy.ApplyMove(move);
                }

                if (SearchAlgorithm(heuristics, moveSets, cubeCopy, newMoves, innerDepth - 1))
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

    private static bool SearchAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSet, Cube cube, List<Move> moves, int depth)
    {
        if (ChecksPass(heuristics, cube))
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        foreach (var set in moveSet)
        {
            foreach (var move in set)
            {
                cube.ApplyMove(move);

                moves.Add(move);
            }

            if (SearchAlgorithm(heuristics, moveSet, cube, moves, depth - 1))
            {
                return true;
            }

            for (var i = 0; i < set.Count; i++)
            {
                cube.UndoMove();

                moves.RemoveAt(moves.Count - 1);
            }
        }

        return false;
    }

    private static bool ChecksPass(List<Func<Cube, bool>> heuristics, Cube cube)
    {
        foreach (var heuristic in heuristics)
        {
            if (! heuristic(cube))
            {
                return false;
            }
        }
        
        return true;
    }

    // private static bool HasGryCorner(Cube cube)
    // {
    //     return HasAlignedYellowCross(cube)
    //            && cube[Face.Left, 2, 2] == Colour.Green
    //            && cube[Face.Front, 0, 2] == Colour.Red
    //            && cube[Face.Down, 0, 0] == Colour.Yellow;
    // }
    //
    // private static bool HasRbyCorner(Cube cube)
    // {
    //     return HasGryCorner(cube)
    //            && cube[Face.Front, 2, 2] == Colour.Red
    //            && cube[Face.Right, 0, 2] == Colour.Blue
    //            && cube[Face.Down, 2, 0] == Colour.Yellow;
    // }
    //
    // private static bool HasGoyCorner(Cube cube)
    // {
    //     return HasRbyCorner(cube)
    //            && cube[Face.Left, 0, 2] == Colour.Green
    //            && cube[Face.Back, 2, 2] == Colour.Orange
    //            && cube[Face.Down, 0, 2] == Colour.Yellow;
    // }
    //
    // private static bool HasBoyCorner(Cube cube)
    // {
    //     return HasGoyCorner(cube)
    //            && cube[Face.Back, 0, 2] == Colour.Orange
    //            && cube[Face.Right, 2, 2] == Colour.Blue
    //            && cube[Face.Down, 2, 2] == Colour.Yellow;
    // }
}
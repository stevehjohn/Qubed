using System.Collections.Concurrent;
using System.Diagnostics;
using RubiksCube.Core.Logic;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public class Solver
{
    private const int MinDepth = 1;

    private const int MaxDepth = 20;

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
            
            stepCallback?.Invoke(_moves);

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
        var totalStopwatch = Stopwatch.StartNew();

        var branchStopwatch = new Stopwatch();

        for (var depth = MinDepth; depth <= MaxDepth; depth++)
        {
            Console.Write(depth);

            branchStopwatch.Restart();

            var found = false;

            List<Move> foundMoves = null;

            var innerDepth = depth;

            Parallel.ForEach(moveSets, new ParallelOptions(), (moveSet, state, index) =>
            {
                var cubeCopy = _cube.Clone();

                var newMoves = new List<Move>(moveSet);

                var algorithmIndices = new List<int> { (int) index };

                foreach (var move in moveSet)
                {
                    cubeCopy.ApplyMove(move);
                }

                var visitedDepths = new Dictionary<(ulong A, ulong B, ulong C), int>();

                if (SearchAlgorithm(heuristics, moveSets, cubeCopy, newMoves, algorithmIndices, visitedDepths, innerDepth - 1))
                {
                    lock (state)
                    {
                        if (foundMoves == null || newMoves.Count < foundMoves.Count)
                        {
                            found = true;
                            
                            foundMoves = new List<Move>(newMoves);
                        }
                    }
                }
            });

            Console.WriteLine($" {branchStopwatch.Elapsed}");

            if (found)
            {
                _moves.AddRange(foundMoves);

                foreach (var move in foundMoves)
                {
                    _cube.ApplyMove(move);
                }

                Console.WriteLine($"\nNew moves: {foundMoves.Count}, duration: {totalStopwatch.Elapsed}");

                stepCallback(foundMoves);

                return true;
            }
        }

        return false;
    }

    private bool SearchAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSet, Cube cube, List<Move> moves, List<int> algorithmIndices, Dictionary<(ulong A, ulong B, ulong C), int> visitedDepths, int depth)
    {
        if (ChecksPass(heuristics, cube))
        {
            return true;
        }

        if (depth == 0)
        {
            return false;
        }

        var key = cube.GetHash();

        if (visitedDepths.TryGetValue(key, out var seenDepth))
        {
            if (seenDepth >= depth)
            {
                return false;
            }

            visitedDepths[key] = depth;
        }
        else
        {
            visitedDepths.Add(key, depth);
        }
        
        for (var s = 0; s < moveSet.Count; s++)
        {
            var set = moveSet[s];

            if (moves.Count > 0)
            {
                var lastMove = moves[^1];

                var move = set[0];

                if (lastMove.Face == move.Face)
                {
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (lastMove.Direction)
                    {
                        case Direction.HalfTurn when move.Direction == Direction.HalfTurn:
                        case Direction.Clockwise when move.Direction == Direction.AntiClockwise:
                        case Direction.AntiClockwise when move.Direction == Direction.Clockwise:
                            continue;
                    }
                }
            }

            var occurrences = 0;

            if (algorithmIndices.Count > 1)
            {
                for (var o = 0; o < algorithmIndices.Count; o++)
                {
                    if (algorithmIndices[o] == s)
                    {
                        occurrences++;
                    }
                }
            }

            if (occurrences > 1)
            {
                continue;
            }
            
            foreach (var move in set)
            {
                cube.ApplyMove(move);

                moves.Add(move);

                if (ChecksPass(heuristics, cube))
                {
                    return true;
                }
            }

            algorithmIndices.Add(s);

            if (SearchAlgorithm(heuristics, moveSet, cube, moves, algorithmIndices, visitedDepths, depth - 1))
            {
                return true;
            }

            for (var i = 0; i < set.Count; i++)
            {
                cube.UndoMove();

                moves.RemoveAt(moves.Count - 1);
            }

            algorithmIndices.RemoveAt(algorithmIndices.Count - 1);
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
}
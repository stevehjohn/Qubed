using System.Diagnostics;
using RubiksCube.Core.Infrastructure;
using RubiksCube.Core.Logic;
using RubiksCube.Core.Models;

namespace RubiksCube.Core;

public sealed class Solver
{
    private const int MinDepth = 1;

    private const int MaxDepth = 20;

    private readonly Cube _cube;

    private readonly ILogger _logger;

    private readonly int _degreeOfParallelism;

    public Solver(Cube cube, Mode mode = Mode.HalfCores) : this(cube, mode, null)
    {
    }

    public Solver(Cube cube, Mode mode, ILogger logger)
    {
        _cube = cube.Clone();

        _degreeOfParallelism = mode switch
        {
            Mode.Fast => Environment.ProcessorCount - 1,
            Mode.HalfCores => Environment.ProcessorCount / 2,
            Mode.SingleThreaded => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        _logger = logger;
    }

    public void SolveAsync(Action<(bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration)> callback, Action<List<Move>> stepCallback = null)
    {
        Task.Run(() => Solve(stepCallback))
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger?.WriteLine(task.Exception?.GetBaseException().ToString());

                    callback((false, [], TimeSpan.Zero));

                    return;
                }

                callback(task.Result);
            });
    }

    public (bool Solved, IReadOnlyList<Move> Moves, TimeSpan Duration) Solve(Action<List<Move>> stepCallback = null)
    {
        _logger?.WriteLine();

        if (_cube.IsSolved())
        {
            _logger?.WriteLine(_cube.ToString());

            stepCallback?.Invoke([]);

            return (true, [], TimeSpan.Zero);
        }

        var stopwatch = Stopwatch.StartNew();

        _logger?.WriteLine(_cube.ToString());

        _logger?.WriteLine();

        var checks = new List<Func<Cube, bool>>();

        var totalNodes = 0;

        var solution = new List<Move>();

        var solved = SearchStages(0, _cube, checks, solution, ref totalNodes);

        _logger?.WriteLine(_cube.ToString());

        _logger?.WriteLine();

        // CompressMoves();

        stopwatch.Stop();

        _logger?.WriteLine();

        // _logger?.WriteLine($"Moves: {_candidates.Count}. Duration: {stopwatch.Elapsed:mm\\:ss\\.fff}, Total nodes explored: {totalNodes:N0}.\n");

        return (solved, solution, stopwatch.Elapsed);
    }

    private bool SearchStages(
        int algorithmIndex,
        Cube cube,
        List<Func<Cube, bool>> checks,
        List<Move> solution,
        ref int totalNodes)
    {
        if (algorithmIndex == AlgorithmLibrary.Algorithms.Count)
        {
            return cube.IsSolved();
        }

        var algorithm = AlgorithmLibrary.Algorithms[algorithmIndex];
        
        Console.WriteLine(algorithm.Name);

        checks.AddRange(algorithm.IsCompleteChecks);

        if (ChecksPass(checks, cube))
        {
            var passed = SearchStages(algorithmIndex + 1, cube, checks, solution, ref totalNodes);

            RemoveChecks(checks, algorithm.IsCompleteChecks.Length);

            return passed;
        }

        var result = BruteForceAlgorithm(checks, algorithm.MoveSets, cube);

        totalNodes += result.NodesExplored;

        foreach (var candidate in result.Candidates.Where(c => c is { Count: > 0 }).OrderBy(c => c.Count))
        {
            var before = solution.Count;

            foreach (var move in candidate)
            {
                cube.ApplyMove(move);
                
                solution.Add(move);
            }

            if (SearchStages(algorithmIndex + 1, cube, checks, solution, ref totalNodes))
            {
                return true;
            }

            while (solution.Count > before)
            {
                cube.UndoMove();
                
                solution.RemoveAt(solution.Count - 1);
            }
        }
        
        RemoveChecks(checks, algorithm.IsCompleteChecks.Length);

        return false;
    }

    private static void RemoveChecks<T>(List<T> checks, int count)
    {
        checks.RemoveRange(checks.Count - count, count);
    }

    private (bool ChecksPass, int NodesExplored, List<List<Move>> Candidates) BruteForceAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSets, Cube cube)
    {
        var totalStopwatch = Stopwatch.StartNew();

        var branchStopwatch = new Stopwatch();

        var nodesExplored = 0;

        var candidates = new List<List<Move>>();

        for (var depth = MinDepth; depth <= MaxDepth; depth++)
        {
            _logger?.Write(depth.ToString());

            branchStopwatch.Restart();

            var found = false;

            var innerDepth = depth;

            var stateLock = new Lock();

            nodesExplored = 0;

            candidates.Clear();

            Parallel.ForEach(moveSets, new ParallelOptions
            {
                MaxDegreeOfParallelism = _degreeOfParallelism
            }, (moveSet, _, index) =>
            {
                var cubeCopy = cube.Clone();

                var newMoves = new List<Move>();

                var algorithmIndices = new List<int> { (int) index };

                // ReSharper disable once AccessToModifiedClosure
                Interlocked.Increment(ref nodesExplored);

                foreach (var move in moveSet)
                {
                    cubeCopy.ApplyMove(move);

                    newMoves.Add(move);

                    if (ChecksPass(heuristics, cubeCopy))
                    {
                        break;
                    }
                }

                var visitedDepths = new Dictionary<(ulong A, ulong B, ulong C), int>();

                if (SearchAlgorithm(heuristics, moveSets, cubeCopy, newMoves, candidates, algorithmIndices, visitedDepths, innerDepth - 1, ref nodesExplored))
                {
                    lock (stateLock)
                    {
                        found = true;

                        candidates.AddRange(newMoves);
                    }
                }
            });

            _logger?.WriteLine($" {branchStopwatch.Elapsed}");

            if (found)
            {
                _logger?.WriteLine($"\nDuration: {totalStopwatch.Elapsed:ss\\.fff}");

                return (true, nodesExplored, candidates);
            }
        }

        return (false, nodesExplored, candidates);
    }

    private static bool SearchAlgorithm(List<Func<Cube, bool>> heuristics, IReadOnlyList<IReadOnlyList<Move>> moveSet, Cube cube, List<Move> moves, List<List<Move>> candidates, List<int> algorithmIndices, Dictionary<(ulong A, ulong B, ulong C), int> visitedDepths, int depth, ref int nodesExplored)
    {
        if (depth == 0)
        {
            return candidates.Count > 0;
        }

        // var key = cube.GetHash();
        //
        // if (visitedDepths.TryGetValue(key, out var seenDepth))
        // {
        //     if (seenDepth >= depth)
        //     {
        //         return false;
        //     }
        //
        //     visitedDepths[key] = depth;
        // }
        // else
        // {
        //     visitedDepths.Add(key, depth);
        // }

        for (var s = 0; s < moveSet.Count; s++)
        {
            var set = moveSet[s];

            Interlocked.Increment(ref nodesExplored);

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

            if (occurrences > 2)
            {
                continue;
            }

            var appliedMoves = 0;

            foreach (var move in set)
            {
                cube.ApplyMove(move);

                moves.Add(move);

                appliedMoves++;

                if (ChecksPass(heuristics, cube))
                {
                    candidates.Add([..moves]);

                    break;
                }
            }

            algorithmIndices.Add(s);

            if (SearchAlgorithm(heuristics, moveSet, cube, moves, candidates, algorithmIndices, visitedDepths, depth - 1, ref nodesExplored))
            {
                candidates.Add([..moves]);
            }

            for (var i = 0; i < appliedMoves; i++)
            {
                cube.UndoMove();

                moves.RemoveAt(moves.Count - 1);
            }

            algorithmIndices.RemoveAt(algorithmIndices.Count - 1);
        }

        return candidates.Count > 0;
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

    // private void CompressMoves()
    // {
    //     var changed = true;
    //
    //     while (changed)
    //     {
    //         changed = false;
    //
    //         for (var i = 0; i < _candidates.Count - 1; i++)
    //         {
    //             var first = _candidates[i];
    //
    //             var second = _candidates[i + 1];
    //
    //             if (first.Face == second.Face)
    //             {
    //                 changed = true;
    //
    //                 var newDirection = GetCompressedDirection(first.Direction, second.Direction);
    //
    //                 _candidates.RemoveRange(i, 2);
    //
    //                 if (newDirection != null)
    //                 {
    //                     var newMove = first with { Direction = newDirection.Value };
    //
    //                     _candidates.Insert(i, newMove);
    //                 }
    //
    //                 break;
    //             }
    //         }
    //     }
    // }
    //
    // private static Direction? GetCompressedDirection(Direction first, Direction second)
    // {
    //     var firstTurns = first switch
    //     {
    //         Direction.Clockwise => 1,
    //         Direction.HalfTurn => 2,
    //         _ => 3
    //     };
    //
    //     var secondTurns = second switch
    //     {
    //         Direction.Clockwise => 1,
    //         Direction.HalfTurn => 2,
    //         _ => 3
    //     };
    //
    //     var totalTurns = (firstTurns + secondTurns) % 4;
    //
    //     return totalTurns switch
    //     {
    //         1 => Direction.Clockwise,
    //         2 => Direction.HalfTurn,
    //         3 => Direction.AntiClockwise,
    //         _ => null
    //     };
    // }
}